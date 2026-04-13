using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace MyPlugins
{
    public class MedicineLineRollUpTrigger_CusomtWF : CodeActivity
    {
        //input parameters
        [RequiredArgument]
        [Input("Medicine InvoiceLine Lookup")]
        [ReferenceTarget("apollo_invoiceline")]
        public InArgument<EntityReference> MedicineInvoiceLine { get; set; } //Medicine InvoiceLine Lookup

        //output parameters
        [RequiredArgument]
        [Output("Invoice Line Amount")]
        [ReferenceTarget("apollo_invoiceline")]
        public OutArgument<Money> InvoiceLineAmount { get; set; } //Invoice Line Amount

        protected override void Execute(CodeActivityContext context)
        {
            //code goes here
            //common code to get service in workflow activity
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>(); //get workflow context
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            //retrieving values of input parameters
            EntityReference medicineInvoiceLineRef = MedicineInvoiceLine.Get(context);

            if(medicineInvoiceLineRef != null)
            {
                CalculateRollupFieldRequest rollupRequest = new CalculateRollupFieldRequest
                {
                    Target = medicineInvoiceLineRef,
                    FieldName = "apollo_totalmedicineamount"
                };
                service.Execute(rollupRequest);

                Entity medicineInvoiceLine = service.Retrieve(medicineInvoiceLineRef.LogicalName, medicineInvoiceLineRef.Id, new ColumnSet("apollo_totalmedicineamount"));
                Money totalMedicineAmount = new Money(medicineInvoiceLine.GetAttributeValue<decimal>("apollo_totalmedicineamount"));

                InvoiceLineAmount.Set(context, totalMedicineAmount);
            }
        }
    }
}
