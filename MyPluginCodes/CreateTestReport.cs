using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class CreateTestReport : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreateTestReport Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    Entity medicalTest = (Entity)context.InputParameters["Target"];

                    if (medicalTest != null && medicalTest.LogicalName.ToLower() == "apollo_medicaltest")
                    {
                        if (context.MessageName == "Create" && context.Stage == 40)
                        {
                            tracingService.Trace("On create of MedicalTest, creating TestReport.");
                            // Plug-in business logic goes here.
                            var medicalTestName = medicalTest.GetAttributeValue<string>("apollo_medicaltest1");

                            EntityReference labInfo = null;

                            if (medicalTest.Contains("apollo_lab"))
                                labInfo = medicalTest.GetAttributeValue<EntityReference>("apollo_lab");

                            Entity labRecord = service.Retrieve(labInfo.LogicalName, labInfo.Id, new ColumnSet("apollo_labtechnician"));

                            EntityReference labTechnician = null;

                            if (labRecord.Contains("apollo_labtechnician"))
                                labTechnician = labRecord.GetAttributeValue<EntityReference>("apollo_labtechnician");
                            
                            Entity testReport = new Entity();
                            testReport.LogicalName = "apollo_testreport";
                            testReport["apollo_name"] = $"{medicalTestName}-Report";
                            testReport["apollo_testdate"] = medicalTest.GetAttributeValue<DateTime>("apollo_testdate");
                            testReport["apollo_labtechnician"] = new EntityReference(labTechnician.LogicalName, labTechnician.Id);
                            testReport["apollo_medicaltest"] = new EntityReference(medicalTest.LogicalName, medicalTest.Id);
                            testReport["apollo_lab"] = new EntityReference(labInfo.LogicalName, labInfo.Id);
                            service.Create(testReport);

                            tracingService.Trace("Test Report created successfully.");

                            medicalTest["statuscode"] = new OptionSetValue(554550001);
                            service.Update(medicalTest);
                            tracingService.Trace("Updated MedicalTest statuscode");
                        }
                        else
                        {
                            tracingService.Trace("Plugin executed on unsupported message or stage.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Target entity is not an Appointment.");
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
