using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TslUmdNet
{
    public class TallyData
    {
        public string Sender { get; set; }
        public short Pbc { get; set; }
        public byte Ver { get; set; }
        public byte Flags { get; set; }
        public short Screen { get; set; }
        public short Index { get; set; }
        public short Control { get; set; }
        public short Length { get; set; }
        public TallyDisplayData Display { get; } = new TallyDisplayData();
    }

    public class TallyDisplayData
    {
        public string Text { get; set; }
        public byte RhTally { get; set; }
        public byte TextTally { get; set; }
        public byte LhTally { get; set; }
        public byte Brightness { get; set; }
        public byte Reserved { get; set; }
        public byte ControlData { get; set; }
    }
}
