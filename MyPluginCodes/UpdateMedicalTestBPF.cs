using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using static MyPlugins.BPFUpdateCode;

namespace MyPlugins
{
    public class UpdateMedicalTestBPF: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            string logicalNameOfBPF = "apollo_medicaltestbpf";
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("UpdateTestReport Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the IOrganizationService instance which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {
                    // Obtain the target entity from the input parameters.  
                    Entity testReport = (Entity)context.InputParameters["Target"];

                    if (testReport != null && testReport.LogicalName.ToLower() == "apollo_testreport")
                    {
                        if (context.MessageName == "Update" && context.Stage == 20)
                        {
                            tracingService.Trace("On update of TestReport, updating MedicalTestBPF.");
                            // Plug-in business logic goes here.
                            Entity preImg = (Entity)context.PreEntityImages["TRPreImage"];

                            EntityReference medicalTest = preImg.GetAttributeValue<EntityReference>("apollo_medicaltest");
                            Entity medicalTestEntity = service.Retrieve(medicalTest.LogicalName, medicalTest.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("statuscode"));
                            Entity activeBPFProcessInstance = GetActiveBPF(medicalTestEntity, service);

                            //Id of the active process instance, which will be used
                            Guid activeBPFId = activeBPFProcessInstance.Id;
                            //Retrieve the active stage ID of in the active process instance
                            Guid activeStageId = new Guid(activeBPFProcessInstance.Attributes["processstageid"].ToString());
                            // Retrieve the stage ID of the next stage that you want to set as active
                            Guid nextStageId = Guid.Empty;

                            if (activeBPFProcessInstance != null && testReport.Contains("apollo_samplecollected") && testReport.GetAttributeValue<Boolean>("apollo_samplecollected"))
                            {
                                medicalTestEntity["statuscode"] = new OptionSetValue(1); //Sample Collected

                                // BPF UPDATE
                                int currentStagePosition = -1;
                                RetrieveActivePathResponse pathResp = GetAllStagesOfSelectedBPF(activeBPFId, activeStageId, ref currentStagePosition, service);
                                if (currentStagePosition > -1 && pathResp.ProcessStages != null && pathResp.ProcessStages.Entities != null && currentStagePosition + 1 < pathResp.ProcessStages.Entities.Count)
                                {
                                 nextStageId = (Guid)pathResp.ProcessStages.Entities[currentStagePosition + 1].Attributes["processstageid"];
                                }  
                            }

                            else if (testReport.Contains("apollo_findings") && testReport.GetAttributeValue<string>("apollo_findings") != "")
                            {
                                testReport["statecode"] = new OptionSetValue(1);
                                testReport["statuscode"] = new OptionSetValue(2); //close the Test Report
                                testReport["apollo_reportdate"] = DateTime.UtcNow;

                                medicalTestEntity["statuscode"] = new OptionSetValue(554550002); //Report Generated

                                //  BPF UPDATE
                                int currentStagePosition = -1;
                                RetrieveActivePathResponse pathResp = GetAllStagesOfSelectedBPF(activeBPFId, activeStageId, ref currentStagePosition, service);
                                if (currentStagePosition > -1 && pathResp.ProcessStages != null && pathResp.ProcessStages.Entities != null && currentStagePosition + 1 < pathResp.ProcessStages.Entities.Count)
                                {
                                    nextStageId = (Guid)pathResp.ProcessStages.Entities[currentStagePosition + 1].Attributes["processstageid"];
                                }
                            }
                            service.Update(medicalTestEntity);

                            Entity entBPF = new Entity(logicalNameOfBPF)
                            {
                                Id = activeBPFId
                            };
                            entBPF["activestageid"] = new EntityReference("processstage", nextStageId);

                            service.Update(entBPF);

                            tracingService.Trace("MedicalTestBPF updated successfully.");
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
