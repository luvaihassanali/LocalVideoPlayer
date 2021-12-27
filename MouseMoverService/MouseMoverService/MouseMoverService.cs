using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MouseMoverService
{
    public partial class MouseMoverService : ServiceBase
    {
        Bootstrap bootstrap;
        public MouseMoverService()
        {
            InitializeComponent();

            bootstrap = new Bootstrap();
        }

        protected override void OnStart(string[] args)
        {
            bootstrap.StartService();
        }

        protected override void OnStop()
        {
            bootstrap.StopService();
        }
    }
}
