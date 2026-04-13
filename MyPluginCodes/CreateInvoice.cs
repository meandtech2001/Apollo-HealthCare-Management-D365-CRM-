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
    public class CreateInvoice : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreateInvoice Plugin Execution Started.");

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
                    Entity appointment = (Entity)context.InputParameters["Target"];

                    if (appointment != null && appointment.LogicalName.ToLower() == "appointment")
                    {
                        if (context.MessageName == "Create" && context.Stage == 40)
                        {
                            tracingService.Trace("On create of Appointment, creating Invoice.");
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

                            Entity invoice = new Entity();
                            invoice.LogicalName = "apollo_apolloinvoice";
                            invoice["apollo_invoicename"] = $"{appointmentNumber}-{patientNumber}/{patientName}-Invoice";
                            invoice["apollo_appointment"] = new EntityReference(appointment.LogicalName, appointment.Id);
                            Guid newInvoice = service.Create(invoice);

                            tracingService.Trace("Invoice created successfully - " + newInvoice);

                            tracingService.Trace("On create of Invoice, creating Consulting Invoice Line");

                            Entity invoiceLine = new Entity();
                            invoiceLine.LogicalName = "apollo_invoiceline";
                            invoiceLine["apollo_invoicelinename"] = $"{appointmentNumber}-{patientNumber}/{patientName}-Consulting";
                            invoiceLine["apollo_amount"] = new Money(250); // Set the amount as needed
                            invoiceLine["apollo_paymentfor"] = new OptionSetValue(554550001); // Consulting
                            invoiceLine["apollo_parentinvoice"] = new EntityReference("apollo_apolloinvoice", newInvoice);
                            service.Create(invoiceLine);

                            tracingService.Trace("Consulting Invoice Line created successfully.");

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
