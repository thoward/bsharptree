namespace bsharptree.example.simpleindex.analysis
{
    using System.Collections.Generic;
    using System.Linq;

    public class StringInverter: Inverter<string, string>
    {
        public override string NormalizeUnit(string unit)
        {
            return unit.ToLower();
        }

        public override IEnumerable<string> Invert(string source)
        {
            return source.Split(' ').Select(NormalizeUnit).Where(a=> !string.IsNullOrWhiteSpace(a)).Distinct();
        }
    }
}