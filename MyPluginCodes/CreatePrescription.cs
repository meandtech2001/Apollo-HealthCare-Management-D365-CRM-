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
    public class CreatePrescription : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =(ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreatePrescription Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext) serviceProvider.GetService(typeof(IPluginExecutionContext));

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
                    Entity appointment = (Entity)context.InputParameters["Target"];

                    if (appointment != null && appointment.LogicalName.ToLower() == "appointment")
                    {
                        if (context.MessageName == "Create" && context.Stage == 40)
                        {
                            tracingService.Trace("On create of Appointment, creating Prescription.");
                            // Plug-in business logic goes here.
                            var patientName = appointment.GetAttributeValue<EntityReference>("apollo_patient")?.Name;
                            string appointmentNumber = appointment.GetAttributeValue<string>("apollo_appointmentnumber");

                            EntityReference patientRef = appointment.GetAttributeValue<EntityReference>("apollo_patient");
                            string patientNumber = string.Empty;

                            if (patientRef != null)
                            {
                                Entity patient = service.Retrieve(patientRef.LogicalName, patientRef.Id, new ColumnSet("apollo_contactnumber"));
                                if (patient.Contains("apollo_contactnumber"))
                                {
                                    patientNumber = patient.GetAttributeValue<string>("apollo_contactnumber");
                                }
                            }

                            Entity prescription = new Entity();
                            prescription.LogicalName = "apollo_prescription";
                            prescription["apollo_name"] = $"{appointmentNumber}-{patientNumber}/{patientName}-Prescription";
                            prescription["apollo_appointment"] = new EntityReference(appointment.LogicalName,appointment.Id);
                            service.Create(prescription);

                            tracingService.Trace("Prescription created successfully.");
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
