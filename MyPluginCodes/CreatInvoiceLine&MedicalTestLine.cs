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
    public class CreatInvoiceLine_MedicalTestLine: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CreatInvoiceLine_MedicalTestLine Plugin Execution Started.");

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
                    Entity medicalTest = (Entity)context.InputParameters["Target"];

                    if (medicalTest != null && medicalTest.LogicalName.ToLower() == "apollo_medicaltest")
                    {
                        if (context.MessageName == "Create" && context.Stage == 40 && context.Depth <= 1)
                        {
                            tracingService.Trace("On create of Medicaltest, creating Invoice Line and MedicalTestinvoicesline.");
                            // Plug-in business logic goes here.

                            EntityReference prescriptionRef = medicalTest.GetAttributeValue<EntityReference>("apollo_prescription");

                            Entity prescription = service.Retrieve(prescriptionRef.LogicalName, prescriptionRef.Id, new ColumnSet("apollo_appointment"));
                            EntityReference appointmentRef = prescription.GetAttributeValue<EntityReference>("apollo_appointment");

                            // Check for existing Invoiceline for MedicalTest for the given Appointment
                            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='apollo_invoiceline'>
                                                <attribute name='apollo_invoicelineid' />
                                                <attribute name='apollo_invoicelinename' />
                                                <attribute name='createdon' />
                                                <order attribute='apollo_invoicelinename' descending='false' />
                                                <filter type='and'>
                                                  <condition attribute='apollo_paymentfor' operator='eq' value='554550002' />
                                                </filter>
                                                <link-entity name='apollo_apolloinvoice' from='apollo_apolloinvoiceid' to='apollo_parentinvoice' link-type='inner' alias='ac'>
                                                  <filter type='and'>
                                                    <condition attribute='apollo_appointment' operator='eq' uitype='appointment' value='{{{appointmentRef.Id}}}' />
                                                  </filter>
                                                </link-entity>
                                              </entity>
                                            </fetch>";

                            EntityCollection invoicelineMedicalTestResults = service.RetrieveMultiple(new FetchExpression(fetchXml));

                            EntityReference invoicelineMedicalTest = null;

                            // If exists, use the existing Invoiceline
                            if (invoicelineMedicalTestResults.Entities.Count > 0)
                            {
                                foreach (Entity invoiceline in invoicelineMedicalTestResults.Entities)
                                {
                                    invoicelineMedicalTest = new EntityReference("apollo_invoiceline", invoiceline.Id);
                                    tracingService.Trace("Existing Invoiceline found for MedicalTest.");
                                    break;
                                }
                            }

                            // If not exists, create a new Invoiceline for MedicalTest
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
                                        tracingService.Trace("Creating a new Invoiceline MedicalTest");
                                        Entity invoiceline = new Entity();
                                        invoiceline.LogicalName = "apollo_invoiceline";
                                        invoiceline["apollo_invoicelinename"] = $"{invoice.GetAttributeValue<string>("apollo_invoicename")}-MedicalTestLine";
                                        invoiceline["apollo_paymentfor"] = new OptionSetValue(554550002); // MedicalTest
                                        invoiceline["apollo_parentinvoice"] = new EntityReference(invoice.LogicalName, invoice.Id);
                                        newInvoiceLineId = service.Create(invoiceline);
                                        tracingService.Trace("Invoiceline created successfully.");
                                    }
                                }
                                // Get the reference to the newly created MedicalTestInvoiceline
                                invoicelineMedicalTest = new EntityReference("apollo_invoiceline", newInvoiceLineId);
                            }

                            // Now create MedicalTestinvoicesline record linking the MedicalTest and the Invoiceline
                            string medicalTestName = medicalTest.GetAttributeValue<string>("apollo_medicaltest1");

                            tracingService.Trace("Creating MedicalTestinvoicesline.");

                            Entity medicalTestinvoicesline = new Entity();
                            medicalTestinvoicesline.LogicalName = "apollo_medicaltestinvoicelines";
                            medicalTestinvoicesline["apollo_newcolumn"] = $"{medicalTestName}-MedicalTestLine";
                            medicalTestinvoicesline["apollo_invoiceline"] = new EntityReference("apollo_invoiceline", invoicelineMedicalTest.Id);
                            medicalTestinvoicesline["apollo_medicaltest"] = new EntityReference("apollo_medicaltest", medicalTest.Id);
                            service.Create(medicalTestinvoicesline);

                            tracingService.Trace("MedicalTestinvoicesline created successfully.");
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
