using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ISavable
{
    object CaptureState();
    void RestoreState(object state);
}
