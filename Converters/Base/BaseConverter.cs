﻿using System;
using System.Windows.Markup;

namespace GeeksWithBlogsToMarkdown.Converters.Base
{
    public abstract class BaseConverter : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}