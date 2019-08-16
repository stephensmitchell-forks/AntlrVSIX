﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntlrVSIX.Extensions;
using AntlrVSIX.Grammar;
using AntlrVSIX.Package;
using AntlrVSIX.Tagger;

namespace AntlrVSIX.AggregateTagger
{
    class AntlrToClassifierName
    {
        public static Dictionary<AntlrTagTypes, string> Map = new Dictionary<AntlrTagTypes, string>()
        {
            { AntlrTagTypes.Nonterminal, AntlrVSIX.Constants.ClassificationNameNonterminal},
            { AntlrTagTypes.Terminal, AntlrVSIX.Constants.ClassificationNameTerminal },
            { AntlrTagTypes.Comment, AntlrVSIX.Constants.ClassificationNameComment },
            { AntlrTagTypes.Keyword, AntlrVSIX.Constants.ClassificationNameKeyword },
            { AntlrTagTypes.Literal, AntlrVSIX.Constants.ClassificationNameLiteral },
            { AntlrTagTypes.Mode, AntlrVSIX.Constants.ClassificationNameMode },
            { AntlrTagTypes.Channel, AntlrVSIX.Constants.ClassificationNameChannel },
        };
        public static Dictionary<AntlrTagTypes, System.Windows.Media.Color> MapColor = new Dictionary<AntlrTagTypes, System.Windows.Media.Color>()
        {
            { AntlrTagTypes.Nonterminal, AntlrVSIX.Constants.NormalColorTextForegroundNonterminal},
            { AntlrTagTypes.Terminal, AntlrVSIX.Constants.NormalColorTextForegroundTerminal },
            { AntlrTagTypes.Comment, AntlrVSIX.Constants.NormalColorTextForegroundComment },
            { AntlrTagTypes.Keyword, AntlrVSIX.Constants.NormalColorTextForegroundKeyword },
            { AntlrTagTypes.Literal, AntlrVSIX.Constants.NormalColorTextForegroundLiteral },
            { AntlrTagTypes.Mode, AntlrVSIX.Constants.NormalColorTextForegroundMode },
            { AntlrTagTypes.Channel, AntlrVSIX.Constants.NormalColorTextForegroundChannel },
        };
        public static Dictionary<AntlrTagTypes, System.Windows.Media.Color> MapInvertedColor = new Dictionary<AntlrTagTypes, System.Windows.Media.Color>()
        {
            { AntlrTagTypes.Nonterminal, AntlrVSIX.Constants.InvertedColorTextForegroundNonterminal},
            { AntlrTagTypes.Terminal, AntlrVSIX.Constants.InvertedColorTextForegroundTerminal },
            { AntlrTagTypes.Comment, AntlrVSIX.Constants.InvertedColorTextForegroundComment },
            { AntlrTagTypes.Keyword, AntlrVSIX.Constants.InvertedColorTextForegroundKeyword },
            { AntlrTagTypes.Literal, AntlrVSIX.Constants.InvertedColorTextForegroundLiteral },
            { AntlrTagTypes.Mode, AntlrVSIX.Constants.InvertedColorTextForegroundMode },
            { AntlrTagTypes.Channel, AntlrVSIX.Constants.InvertedColorTextForegroundChannel },
        };
    }
}
