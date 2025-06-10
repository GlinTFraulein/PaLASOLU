using UnityEngine;
using nadena.dev.ndmf;
using PaLASOLU;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
    public class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).Run("PaLASOLU LfUploder Core Process", ctx =>
            {
                Debug.Log("[PaLASOLU] testlog");
            });
        }
    }
}