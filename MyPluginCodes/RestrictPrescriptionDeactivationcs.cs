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
    public class RestrictPrescriptionDeactivationcs : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreatePrescription Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext) serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity prescription = (Entity)context.InputParameters["Target"];

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    if (prescription != null && prescription.LogicalName.ToLower() == "apollo_prescription")
                    {
                        if (context.MessageName == "Update" && context.Stage == 20)
                        {
                            tracingService.Trace("On update of Prescription, check if Medicines are added or not and all Medical Tests are deactivated.");
                            // Plug-in business logic goes here.

                            if (prescription.Contains("statecode") && prescription.GetAttributeValue<OptionSetValue>("statecode").Value == 1)
                            {
                                //fetch medicines and inactive tests
                                QueryExpression queryPrescribedMedicinesquery = new QueryExpression("apollo_prescribedmedicine")
                                {
                                    ColumnSet = new ColumnSet("apollo_prescribedmedicineid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            // Replace 'apollo_prescriptionid' with the logical name of the lookup field on the child table
                                            new ConditionExpression("apollo_prescription", ConditionOperator.Equal, prescription.Id)
                                        }
                                    }
                                };

                                QueryExpression queryTests = new QueryExpression("apollo_medicaltest")
                                {
                                    ColumnSet = new ColumnSet("apollo_medicaltestid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("apollo_prescription", ConditionOperator.Equal, prescription.Id)
                                        }
                                    }
                                };

                                QueryExpression queryInactiveTests = new QueryExpression("apollo_medicaltest")
                                {
                                    ColumnSet = new ColumnSet("apollo_medicaltestid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("apollo_prescription", ConditionOperator.Equal, prescription.Id),
                                            new ConditionExpression("statecode", ConditionOperator.Equal, 1) // Inactive state
                                        }
                                    }
                                };

                                EntityCollection prescribemedicines = service.RetrieveMultiple(queryPrescribedMedicinesquery);
                                EntityCollection Tests = service.RetrieveMultiple(queryTests);
                                EntityCollection inactiveTests = service.RetrieveMultiple(queryInactiveTests);

                                if((prescribemedicines.Entities.Count == 0 && Tests.Entities.Count == 0) || (Tests.Entities.Count != inactiveTests.Entities.Count))
                                {
                                    throw new InvalidPluginExecutionException("Cannot deactivate Prescription without adding Medicines and deactivating all Medical Tests.");
                                }
                            }       
                        }
                        else
                        {
                            tracingService.Trace("Plugin executed on unsupported message or stage.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Target entity is not an Prescription.");
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
