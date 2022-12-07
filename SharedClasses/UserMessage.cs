using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DefaultNamespace;


class MessageFromClient
    {
        public List<Point> Points { get; set; } = new List<Point>();
        public String Mechanism { get; set; }
        public int NumberOfTasks { get; set; }
        public int TimePhase1 { get; set; }
        public int TimePhase2 { get; set; }
    }
}