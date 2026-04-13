using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class UpdateTestReport : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("UpdateTestReport Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    Entity medicaltest = (Entity)context.InputParameters["Target"];

                    if (medicaltest != null && medicaltest.LogicalName.ToLower() == "apollo_medicaltest")
                    {
                        if (context.MessageName == "Update" && context.Stage == 40)
                        {
                            tracingService.Trace("On update of MedicalTest, updating TestReport.");
                            // Plug-in business logic goes here.

                            Entity preImg = context.PreEntityImages["MTPreImage"];

                            string medicaltestName = preImg.GetAttributeValue<string>("apollo_medicaltest1");

                            var fetchXML = $@"<fetch version='1.0' mapping='logical' savedqueryid='0fe5dc4e-25fa-4415-b5ae-e213df8daea6' no-lock='false' distinct='true'><entity name='apollo_testreport'><attribute name='apollo_testreportid'/><attribute name='apollo_name'/><attribute name='apollo_lab'/><attribute name='apollo_labtechnician'/><attribute name='apollo_testdate'/><attribute name='apollo_medicaltest'/><filter type='and'><condition attribute='statecode' operator='eq' value='0'/><condition attribute='apollo_medicaltest' operator='eq' value='{medicaltest.Id}' uiname='{medicaltestName}' uitype='apollo_medicaltest'/></filter></entity></fetch>";

                            EntityCollection testReportRecords = service.RetrieveMultiple(new FetchExpression(fetchXML));

                            if (medicaltest.Contains("apollo_lab"))
                            {
                                foreach (var testRep in testReportRecords.Entities)
                                {
                                    EntityReference labInfo = medicaltest.GetAttributeValue<EntityReference>("apollo_lab");

                                    Entity labRecord = service.Retrieve(labInfo.LogicalName, labInfo.Id, new ColumnSet("apollo_labtechnician"));

                                    EntityReference labTechnician = null;

                                    if (labRecord.Contains("apollo_labtechnician"))
                                        labTechnician = labRecord.GetAttributeValue<EntityReference>("apollo_labtechnician");

                                    testRep["apollo_lab"] = labInfo;
                                    testRep["apollo_labtechnician"] = labTechnician;

                                    service.Update(testRep);
                                }
                            }
                            else if (medicaltest.Contains("apollo_testdate"))
                            {
                                foreach (var testRep in testReportRecords.Entities)
                                {
                                    testRep["apollo_testdate"] = medicaltest.GetAttributeValue<DateTime>("apollo_testdate");
                                    service.Update(testRep);
                                }
                            }
                            else if (medicaltest.Contains("apollo_lab") && medicaltest.Contains("apollo_prescriptionissuedon"))
                            {
                                foreach (var testRep in testReportRecords.Entities)
                                {
                                    EntityReference labInfo = medicaltest.GetAttributeValue<EntityReference>("apollo_lab");

                                    Entity labRecord = service.Retrieve(labInfo.LogicalName, labInfo.Id, new ColumnSet("apollo_labtechnician"));

                                    EntityReference labTechnician = null;

                                    if (labRecord.Contains("apollo_labtechnician"))
                                        labTechnician = labRecord.GetAttributeValue<EntityReference>("apollo_labtechnician");

                                    testRep["apollo_lab"] = labInfo;
                                    testRep["apollo_labtechnician"] = labTechnician;
                                    testRep["apollo_testdate"] = medicaltest.GetAttributeValue<DateTime>("apollo_testdate");

                                    service.Update(testRep);
                                }
                            }

                            tracingService.Trace("TestReport updated successfully.");
                        }
                        else
                        {
                            tracingService.Trace("Plugin executed on unsupported message or stage.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Target entity is not MedicalTest.");
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in Plugin.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Plugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
