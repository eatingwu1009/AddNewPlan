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
        public VMS.TPS.Common.Model.API.Image SI { get; set; }
        public StructureSet SS { get; set; }
        public List<String> Markers { get; set; }
        public UserControl1(ScriptContext scriptContext)
        {
            SC = scriptContext;
            SS = SC.StructureSet;
            Markers = new List<string>();

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
            SI = SC.Image;
            Course course = SC.Patient.Courses.Where(s => s.Id =="AutoLoad").FirstOrDefault();
            if (course is null)
            {
                course = SC.Patient.AddCourse();
                course.Id = "AutoLoad";
            }

            StructureSet ss = SC.Patient.StructureSets.Where(s => s.Id == SI.Id).FirstOrDefault();
            if (ss is null)
            {
                ss = SI.CreateNewStructureSet();
                ss.Id = SI.Id;
            }
            ExternalPlanSetup plan = course.AddExternalPlanSetup(ss);

            if (ss.Id.Length < 9) { plan.Id = "Load" + ss.Id; } else { plan.Id = ss.Id; }
            var BodyPar = ss.GetDefaultSearchBodyParameters();
            ss.CreateAndSearchBody(BodyPar);

            string LINACID = machine.SelectedItem.ToString();
            ExternalBeamMachineParameters beamparams = new ExternalBeamMachineParameters(LINACID, "6X", 600, "STATIC", null);
            VVector isocenter = new VVector(SC.Image.UserOrigin.x, SC.Image.UserOrigin.y, SC.Image.UserOrigin.z);
            plan.AddMLCBeam(beamparams, null, new VRect<double>(-50, -50, 50, 50), 0, 0, 0, isocenter);

            var myDRR = new DRRCalculationParameters(500); // 500mm is the DRR size
            myDRR.SetLayerParameters(0, 0.1, -550, 0, -100, 100); // Layer 1
            myDRR.SetLayerParameters(1, 0.9, 100, 1000, -100, 100); // Layer 2

            foreach (Beam beam in plan.Beams)
            { beam.CreateOrReplaceDRR(myDRR);}

            Window.GetWindow(this).Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Markers.Add("User Origin\tx:" + SC.Image.UserOrigin.x + "\ty:" + SC.Image.UserOrigin.y + "\tz:" + SC.Image.UserOrigin.y);
            foreach(Structure a in SS.Structures.Where(s => s.DicomType == "MARKER"))
            {
                Markers.Add("\n" + a.Id + "\tx:"+ a.CenterPoint.x + "\ty:" + a.CenterPoint.y+ "\tz:" + a.CenterPoint.z);
            }
        }
        
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Markers.Clear();
        }
    }

}
