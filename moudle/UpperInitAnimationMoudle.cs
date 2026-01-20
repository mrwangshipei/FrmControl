using FrmControl.moudle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpperComAutoTest.MyControls.Frm;
using UPPERIOC.UPPER.IOC.Center.IProvider;
using UPPERIOC.UPPER.Moudle_;
using UPPERIOC.UPPER.Sendor.Moudle;

namespace FrmControl.moudle
{
    public class UpperInitAnimationMoudle : IUPPERModule, IModulePostConstruction, IModuleInitialization, IModulePostInitialization, IModulePreInitialization
    {
        public override Type[] Dependencies =>Type.EmptyTypes;
        TextLoading tx;
        private int loadstatus;
        public void OnPreInitialize(IContainerProvider provider)
        {
            tx = new TextLoading((ms) => {
                while (loadstatus < 4)
                {
                    ms.SetMessage(loadstatus * 25, "核心组件正在初始化");
                    Thread.Sleep(19);
                }
            });
            tx.StartPosition = FormStartPosition.CenterScreen;
            tx.TopMost = true;
            tx.Show();
            loadstatus = 1;
        }
        public void OnInitialize(IContainerProvider provider)
        {
            loadstatus = 2;
        }
        public void OnPostConstruct(IContainerProvider provider)
        {
            loadstatus = 3;
        }
        public void OnPostInitialize(IContainerProvider provider)
        {
            loadstatus = 4;
        }

       



    }

}
namespace UPPERIOC.UPPER.IOC.Center.Configuation
{
    public static class UPPERMoudleManager
    {
        public static void FrmInitMoudle(this ModuleConfiguaion md)
        {
            md.AddModule<UpperInitAnimationMoudle>();
        }
    }
}

