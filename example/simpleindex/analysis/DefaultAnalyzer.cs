﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace bsharptree.example.simpleindex.analysis
{
    public class DefaultInverter : Inverter<Guid, Stream, TermLocation>
    {
        public IEnumerable<IInversionUnit<Guid, TermLocation>> Invert(IInvertable<Guid, Stream, TermLocation> intervable)
        {
            var stream = intervable.Value;
            if (stream.CanRead)
            {
                var scanner = new Utf8CharScanner(stream);

                CharLocation charLocation;
                long lastNonWhitespaceSpanStart = -1;
                long lastNonWhitespaceSpanEnd = -1;
                var stringBuilder = new StringBuilder();
                do
                {
                    charLocation = scanner.Read();
                    if (!charLocation.Value.HasValue || Char.IsWhiteSpace(charLocation.Value.Value))
                    {
                        if (lastNonWhitespaceSpanStart == -1)
                            continue;

                        yield return
                            new DocumentLocation
                                {
                                    InvertableKey = intervable.Id,
                                    Unit =
                                        new TermLocation
                                            {
                                                Term = stringBuilder.ToString(),
                                                Span =
                                                    new Span
                                                        {Start = lastNonWhitespaceSpanStart, End = lastNonWhitespaceSpanEnd}
                                            }
                                }
                            ;
                        lastNonWhitespaceSpanStart = -1;
                        stringBuilder.Length = 0;
                    }
                    else
                    {
                        if (lastNonWhitespaceSpanStart == -1)
                            lastNonWhitespaceSpanStart = charLocation.ByteSpan.Start;

                        stringBuilder.Append(charLocation.Value.Value);
                        lastNonWhitespaceSpanEnd = charLocation.ByteSpan.End;
                    }
                }
                while (charLocation.Value.HasValue);

                //if (lastNonWhitespaceSpanStart != -1)
                //{
                //    yield return new TermLocation
                //    {
                //        Term = stringBuilder.ToString(),
                //        Span = new Span { Start = lastNonWhitespaceSpanStart, End = lastNonWhitespaceSpanEnd }
                //    };
                //}
            }

        }

        public IEnumerable<DocumentLocation> Invert(Document source)
        {
            throw new NotImplementedException();
        }

        public DocumentLocation NormalizeUnit(DocumentLocation unit)
        {
            throw new NotImplementedException();
        }
    }
}