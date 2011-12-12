namespace bsharptree.example.simpleindex.analysis
{
    using System.Collections.Generic;
    using System.Linq;

    public abstract class Inverter<TSource, TUnit> : IInverter<TSource, TUnit>
    {
        public IEnumerable<IInversionUnit<TInvertableKey, TUnit>> Invert<TInvertableKey>(IInvertable<TInvertableKey, TSource, TUnit> intervable)
        {
            var source = intervable.Value;

            var inversionUnits = Invert(source);

            return inversionUnits.Select(
                unit => 
                new InversionUnit<TInvertableKey, TUnit>
                    {
                        InvertableKey = intervable.Id, 
                        Unit = unit 
                    });
        }

        public abstract IEnumerable<TUnit> Invert(TSource source);

        public abstract TUnit NormalizeUnit(TUnit unit);

    }
}