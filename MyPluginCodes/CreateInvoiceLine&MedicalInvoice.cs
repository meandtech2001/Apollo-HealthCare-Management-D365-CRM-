using Microsoft.Crm.Sdk;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace MyPlugins
{
    public class CreateMedicalInvoice : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreateMedicalInvoice Plugin Execution Started.");

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
                    Entity presMedicine = (Entity)context.InputParameters["Target"];

                    if (presMedicine != null && presMedicine.LogicalName.ToLower() == "apollo_prescribedmedicine")
                    {
                        if(context.MessageName == "Create" && context.Stage == 20 && context.Depth <= 1) // Pre-Operation Set Prescribedmedicine Name
                        {
                            tracingService.Trace("On create of Prescribedmedicine, Set PrescribedmedicineName");

                            EntityReference prescriptionRef = presMedicine.GetAttributeValue<EntityReference>("apollo_prescription");
                            Entity prescription = service.Retrieve(prescriptionRef.LogicalName, prescriptionRef.Id, new ColumnSet("apollo_name"));

                            EntityReference medicineRef = presMedicine.GetAttributeValue<EntityReference>("apollo_medicines");
                            Entity medicine = service.Retrieve(medicineRef.LogicalName, medicineRef.Id, new ColumnSet("apollo_medicinename"));

                            string prescriptionName = prescription.GetAttributeValue<string>("apollo_name");
                            string prescriptionNameTrim = prescriptionName.Replace("-Prescription", "");

                            presMedicine["apollo_name"] = $"{prescriptionNameTrim}-{medicine.GetAttributeValue<string>("apollo_medicinename")}";
                        }
                        else if (context.MessageName == "Create" && context.Stage == 40 && context.Depth <= 1) // Post-Operation Create InvoiceLine and Medicineinvoicesline
                        {
                            tracingService.Trace("On create of Prescribedmedicine, creating Invoice Line and Medicineinvoicesline.");
                            // Plug-in business logic goes here.

                            EntityReference prescriptionRef = presMedicine.GetAttributeValue<EntityReference>("apollo_prescription");

                            Entity prescription = service.Retrieve(prescriptionRef.LogicalName, prescriptionRef.Id, new ColumnSet("apollo_appointment"));
                            EntityReference appointmentRef = prescription.GetAttributeValue<EntityReference>("apollo_appointment");

                            // Check for existing Invoiceline for Medicine for the given Appointment
                            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='apollo_invoiceline'>
                                                <attribute name='apollo_invoicelineid' />
                                                <attribute name='apollo_invoicelinename' />
                                                <attribute name='createdon' />
                                                <order attribute='apollo_invoicelinename' descending='false' />
                                                <filter type='and'>
                                                  <condition attribute='apollo_paymentfor' operator='eq' value='554550000' />
                                                </filter>
                                                <link-entity name='apollo_apolloinvoice' from='apollo_apolloinvoiceid' to='apollo_parentinvoice' link-type='inner' alias='ac'>
                                                  <filter type='and'>
                                                    <condition attribute='apollo_appointment' operator='eq' uitype='appointment' value='{{{appointmentRef.Id}}}' />
                                                  </filter>
                                                </link-entity>
                                              </entity>
                                            </fetch>";

                            EntityCollection invoicelineMedicineResults = service.RetrieveMultiple(new FetchExpression(fetchXml));

                            EntityReference invoicelineMedicine = null;

                            // If exists, use the existing Invoiceline
                            if (invoicelineMedicineResults.Entities.Count > 0)
                            {
                                foreach(Entity invoiceline in invoicelineMedicineResults.Entities)
                                {
                                    invoicelineMedicine = new EntityReference("apollo_invoiceline", invoiceline.Id);
                                    tracingService.Trace("Existing Invoiceline found for Medicine.");
                                    break;
                                }
                            }

                            // If not exists, create a new Invoiceline for Medicine
                            else
                            {
                                var fetchXmlInvoice = $@" <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='apollo_apolloinvoice'>
                                                        <attribute name='apollo_apolloinvoiceid' />
                                                        <attribute name='apollo_invoicename' />
                                                        <attribute name='apollo_appointment' />
                                                        <filter type='and'>
                                                          <condition attribute='apollo_appointment' operator='eq' uitype='appointment' value='{{{appointmentRef.Id}}}' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";

                                EntityCollection invoiceResults = service.RetrieveMultiple(new FetchExpression(fetchXmlInvoice));

                                Guid newInvoiceLineId = new Guid();

                                if (invoiceResults.Entities.Count > 0)
                                {
                                    foreach (Entity invoice in invoiceResults.Entities)
                                    {
                                        tracingService.Trace("Creating a new Invoiceline Medicine");
                                        Entity invoiceline = new Entity();
                                        invoiceline.LogicalName = "apollo_invoiceline";
                                        invoiceline["apollo_invoicelinename"] = $"{invoice.GetAttributeValue<string>("apollo_invoicename")}-MedicineLine";
                                        invoiceline["apollo_paymentfor"] = new OptionSetValue(554550000); // Medicine
                                        invoiceline["apollo_parentinvoice"] = new EntityReference(invoice.LogicalName, invoice.Id);
                                        newInvoiceLineId = service.Create(invoiceline);
                                        tracingService.Trace("Invoiceline created successfully.");
                                    }
                                }
                                // Get the reference to the newly created MedicineInvoiceline
                                invoicelineMedicine = new EntityReference("apollo_invoiceline", newInvoiceLineId);
                            }

                            // Now create Medicineinvoicesline
                            EntityReference medicineRef = presMedicine.GetAttributeValue<EntityReference>("apollo_medicines");
                            decimal unitPrice = 0;
                            if (medicineRef != null)
                            {
                                Entity medicine = service.Retrieve(medicineRef.LogicalName, medicineRef.Id, new ColumnSet("apollo_unitamount"));
                                if (medicine.Contains("apollo_unitamount"))
                                {
                                    unitPrice = medicine.GetAttributeValue<Money>("apollo_unitamount").Value;
                                }
                            }

                            string presMedicineName = presMedicine.GetAttributeValue<string>("apollo_name");

                            tracingService.Trace("Creating Medicineinvoicesline."); 
                            
                            Entity medicineinvoicesline = new Entity();
                            medicineinvoicesline.LogicalName = "apollo_medicineinvoicesline";
                            medicineinvoicesline["apollo_name"] = $"{presMedicineName}-InvoiceLine";
                            medicineinvoicesline["apollo_prescribedmedicine"] = new EntityReference(presMedicine.LogicalName, presMedicine.Id);
                            medicineinvoicesline["apollo_unitprice"] = new Money(unitPrice);
                            medicineinvoicesline["apollo_invoiceline"] = new EntityReference("apollo_invoiceline", invoicelineMedicine.Id);
                            medicineinvoicesline["apollo_prescription"] = new EntityReference("apollo_prescription", prescriptionRef.Id);
                            service.Create(medicineinvoicesline);

                            tracingService.Trace("Medicineinvoicesline created successfully.");
                        }
                        else
                        {
                            tracingService.Trace("Plugin executed on unsupported message or stage.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Target entity is not an Prescribedmedicine.");
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
