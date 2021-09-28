using Common.XO.Requests;
using System.Threading.Tasks;
using static Common.Execution.Modes;

namespace DEVICE_CORE
{
    public interface IDeviceApplication
    {
        void Initialize(string pluginPath);
        Task Run(Execution executionMode, string healthCheckValidationMode);
        Task Command(LinkDeviceActionType action);
        void Shutdown();
    }
}
