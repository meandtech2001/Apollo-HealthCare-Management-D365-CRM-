using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using static MyPlugins.BPFUpdateCode;

namespace MyPlugins
{
    public class FinishMedicalTestBPF: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            string logicalNameOfBPF = "apollo_medicaltestbpf";
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("FinishMedicalTestBPF Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext) serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the IOrganizationService instance which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target"))
            {
                try
                {
                    // Obtain the target entity from the input parameters. 
                    EntityReference medicalTest = (EntityReference)context.InputParameters["Target"];

                    Entity medicalTestEntity = service.Retrieve(medicalTest.LogicalName, medicalTest.Id, new ColumnSet("statecode","statuscode"));

                    if (medicalTest != null && medicalTest.LogicalName.ToLower() == "apollo_medicaltest")
                    {
                        if (context.MessageName == "apollo_MedicalTest_FinishBPF" && context.Stage == 40 && context.Depth <= 1)
                        {
                            tracingService.Trace("On verification of Reports, Finish BPF and Deactivate MedicalTest");
                            // Plug-in business logic goes here.
                            
                                Entity activeBPFProcessInstance = GetActiveBPF(medicalTestEntity, service);
                                //Id of the active process instance, which will be used
                                Guid activeBPFId = activeBPFProcessInstance.Id;
                                //Retrieve the active stage ID of in the active process instance
                                Guid activeStageId = new Guid(activeBPFProcessInstance.Attributes["processstageid"].ToString());
                                
                                int currentStagePosition = -1;
                                RetrieveActivePathResponse pathResp = GetAllStagesOfSelectedBPF(activeBPFId, activeStageId, ref currentStagePosition, service);
                                bool isLastStage = (currentStagePosition == pathResp.ProcessStages.Entities.Count - 1);

                                if (isLastStage)
                                {
                                    tracingService.Trace("BPF is in the final stage. Proceeding to finish.");

                                    // 2. Deactivate the Medical Test record
                                    medicalTestEntity["statecode"] = new OptionSetValue(1); // Inactive
                                    medicalTestEntity["statuscode"] = new OptionSetValue(2); // Reports Verified

                                    // 3. Finish the BPF Instance
                                    Entity entBPF = service.Retrieve(logicalNameOfBPF, activeBPFId, new ColumnSet("statecode", "statuscode"));
                                    entBPF["statecode"] = new OptionSetValue(1); // Inactive
                                    entBPF["statuscode"] = new OptionSetValue(2); // Finished

                                    service.Update(entBPF);
                                    service.Update(medicalTestEntity);
                                    tracingService.Trace("Deactivated MedicalTest and BPF");
                                }
                            
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
