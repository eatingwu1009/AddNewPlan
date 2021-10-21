using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AddNewPlan
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public List<String> MachineName { get; set; }
        public ScriptContext SC { get; set; }
        public UserControl1(ScriptContext scriptContext)
        {
            SC = scriptContext;

            MachineName = new List<String>();
            MachineName.Add("LA3TB1623");
            MachineName.Add("LA5TB2069");
            MachineName.Add("LA6TB4313");
            MachineName.Add("LA7TB4557");
            MachineName.Add("iX 1100");

            InitializeComponent();
            DataContext = this;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SC.Patient.BeginModifications();

            Course course = SC.Patient.Courses.Where(s => s.Id =="AutoLoad").FirstOrDefault();
            if (course is null)
            {
                course = SC.Patient.AddCourse();
                course.Id = "AutoLoad";
            }

            StructureSet ss = SC.StructureSet;
            ExternalPlanSetup plan = course.AddExternalPlanSetup(ss);
            if (ss.Id.Length < 9) { plan.Id = "Load" + ss.Id; } else { plan.Id = ss.Id; }

            string LINACID = machine.SelectedItem.ToString();
            ExternalBeamMachineParameters beamparams = new ExternalBeamMachineParameters(LINACID, "6X", 600, "STATIC", null);
            VVector isocenter = new VVector(Math.Round(SC.Image.UserOrigin.x, 0), Math.Round(SC.Image.UserOrigin.y, 0), Math.Round(SC.Image.UserOrigin.z, 0));
            plan.AddMLCBeam(beamparams, null, new VRect<double>(-50, -50, 50, 50), 0, 0, 0, isocenter);
            var myDRR = new DRRCalculationParameters(500); // 500mm is the DRR size
            myDRR.SetLayerParameters(0, 0.1, -550, 0, -100, 100); // Layer 1
            myDRR.SetLayerParameters(1, 0.9, 100, 1000, -100, 100); // Layer 2
            foreach (Beam beam in plan.Beams)
            { beam.CreateOrReplaceDRR(myDRR);}
            Window.GetWindow(this).Close();
        }
    }

}
