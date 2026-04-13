using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class BPFUpdateCode
    {
        public static Entity GetActiveBPF(Entity entity, IOrganizationService crmService)

        {
            Entity activeProcessInstance = null;

            RetrieveProcessInstancesRequest entityBPFsRequest = new RetrieveProcessInstancesRequest
            {
                EntityId = entity.Id,
                EntityLogicalName = entity.LogicalName
            };

            RetrieveProcessInstancesResponse entityBPFsResponse =
                (RetrieveProcessInstancesResponse)crmService.Execute(entityBPFsRequest);

            // Declare variables to store values returned in response
            if (entityBPFsResponse.Processes != null && entityBPFsResponse.Processes.Entities != null)
            {
                int processCount = entityBPFsResponse.Processes.Entities.Count;
                activeProcessInstance = entityBPFsResponse.Processes.Entities[0];
            }
            return activeProcessInstance;
        }

        public static RetrieveActivePathResponse GetAllStagesOfSelectedBPF(Guid activeBPFId, Guid activeStageId,
            ref int currentStagePosition, IOrganizationService crmService)
        {
            // Retrieve the process stages in the active path of the current process instance
            RetrieveActivePathRequest pathReq = new RetrieveActivePathRequest
            {
                ProcessInstanceId = activeBPFId
            };
            RetrieveActivePathResponse pathResp = (RetrieveActivePathResponse)crmService.Execute(pathReq);
            for (int i = 0; i < pathResp.ProcessStages.Entities.Count; i++)
            {
                // Retrieve the active stage name and active stage position based on the activeStageId for the process instance
                if (pathResp.ProcessStages.Entities[i].Attributes["processstageid"].ToString() ==
                    activeStageId.ToString())
                {
                    currentStagePosition = i;
                }
            }
            return pathResp;
        }
    }
}
