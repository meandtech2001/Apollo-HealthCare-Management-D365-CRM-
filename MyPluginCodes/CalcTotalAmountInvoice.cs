using Microsoft.Crm.Sdk.Messages;
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
    public class CalcTotalAmountInvoice : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("CalcTotalAmountInvoice Plugin Execution Started.");

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

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
                    EntityReference invoice = (EntityReference)context.InputParameters["Target"];

                    if (invoice != null)
                    {
                        if (context.MessageName == "apollo_Calc_InsuranceClaim" && context.Depth <= 2 && context.Stage == 40)
                        {
                            tracingService.Trace("Calculating Total Amount using custom action & WF");
                            // Plug-in business logic goes here.
                            Entity invoiceEntity = service.Retrieve(invoice.LogicalName, invoice.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("apollo_totalamount"));

                            //fetch invoice lines related to this invoice
                            tracingService.Trace("Fetching Invoice Lines for Invoice Id: " + invoice.Id.ToString());

                            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='apollo_invoiceline'>
                                                <attribute name='apollo_invoicelineid' />
                                                <attribute name='apollo_invoicelinename' />
                                                <attribute name='apollo_paymentfor' />
                                                <filter type='and'>
                                                  <condition attribute='apollo_parentinvoice' operator='eq' uitype='apollo_apolloinvoice' value='{{{invoice.Id}}}' />
                                                </filter>
                                              </entity>
                                            </fetch>";
                            EntityCollection invoiceLines = service.RetrieveMultiple(new FetchExpression(fetchXml));
                            if(invoiceLines.Entities.Count > 0)
                            {
                                foreach (Entity line in invoiceLines.Entities)
                                {
                                    //Recalculate the rollup fields on each invoice line
                                    if(line.Contains("apollo_paymentfor"))
                                    {
                                        OptionSetValue paymentFor = line.GetAttributeValue<OptionSetValue>("apollo_paymentfor");
                                        List<string> linerollupFields = new List<string>();
                                        //Line is for Medicine or Medical Test
                                        if (paymentFor.Value == 554550000) 
                                           linerollupFields.Add("apollo_totalmedicineamount");
                                        
                                        if(paymentFor.Value == 554550002)
                                           linerollupFields.Add("apollo_totalamountmedicaltest");

                                        foreach (string field in linerollupFields)
                                        {
                                            CalculateRollupFieldRequest rollupRequest = new CalculateRollupFieldRequest
                                            {
                                                Target = line.ToEntityReference(),
                                                FieldName = field
                                            };
                                            service.Execute(rollupRequest);
                                        }
                                    }
                                }
                            }
                            tracingService.Trace("Invoice Line Rollup fields processed: " + invoiceLines.Entities.Count.ToString());

                            // Recalculate the rollup field to ensure it's up to date
                            tracingService.Trace("Calculating Invoice Rollup Fields.");
                            string[] rollupFields = { "apollo_totalamount", "apollo_totalconsultingcharges", "apollo_totalmedicineamount", "apollo_totalmedicaltestinvoiceamount" };

                            foreach (string field in rollupFields)
                            {
                                CalculateRollupFieldRequest rollupRequest = new CalculateRollupFieldRequest
                                {
                                    Target = invoiceEntity.ToEntityReference(),
                                    FieldName = field
                                };
                                service.Execute(rollupRequest);
                            }
                            tracingService.Trace("Invoice Rollup fields calculated.");

                            Entity invoiceEntityNew = service.Retrieve(invoice.LogicalName, invoice.Id, new ColumnSet("apollo_totalamount"));

                            // Get the updated total amount and calculate final amount
                            Money totalAmount = (Money)invoiceEntityNew.GetAttributeValue<Money>("apollo_totalamount");
                            decimal totalAmountValue = totalAmount.Value;
                            Money finalAmount = null;

                            EntityReference insuranceClaimRef = (EntityReference)context.InputParameters["InsuranceClaim"];
                            if(insuranceClaimRef != null)
                            {
                                Entity insurance = service.Retrieve(insuranceClaimRef.LogicalName, insuranceClaimRef.Id, new ColumnSet("apollo_claimamount"));
                                Money insuranceAmount = insurance.GetAttributeValue<Money>("apollo_claimamount");
                                decimal insuranceValue = insuranceAmount.Value;

                                finalAmount = new Money(totalAmountValue - insuranceValue);
                                context.OutputParameters["InsuranceAmountOut"] = insuranceAmount;
                            }
                            else
                            {
                                finalAmount = new Money(totalAmountValue);
                            }

                            context.OutputParameters["TotalAmountOut"] = finalAmount;

                            tracingService.Trace("Total amount calculated" + finalAmount.Value);
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
