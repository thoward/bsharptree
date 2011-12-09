using System.Collections.Generic;
using System.Linq;

namespace bsharptree.test.mockindex
{
    public class StringInverter: IInverter<string, string>
    {
        public IEnumerable<IInversionUnit<TKey, string>> Invert<TKey>(IInvertable<TKey, string, string> intervable)
        {
            var source = intervable.Value;

            // simple split on space
            var words = source.Split(' ').Select(a=> a.ToLower()).Distinct();
        
            return 
                words.Select(word => 
                             new InversionUnit<TKey, string>
                                 {
                                     Key = intervable.Id, 
                                     Value = word
                                 });
        
        }
    }
}