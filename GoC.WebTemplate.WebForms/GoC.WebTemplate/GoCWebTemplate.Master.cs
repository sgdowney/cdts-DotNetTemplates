﻿using GoC.WebTemplate.Components;
using GoC.WebTemplate.Components.Configs;
using GoC.WebTemplate.Components.Utils.Caching;
using System;
using System.Reflection;
using System.Web;

namespace GoC.WebTemplate.WebForms
{
    public partial class WebTemplateMasterPage : System.Web.UI.MasterPage
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            WebTemplateCore = new Model(new FileContentCacheProvider(HttpRuntime.Cache), new ConfigurationProxy(), new CdtsCacheProvider(HttpRuntime.Cache));
        }

        public Model WebTemplateCore { get; set; }

        /// <summary>
        /// property to hold the version of the template. it will be put as a comment in the html of the master pages. this will help us troubleshoot issues with clients using the template
        /// </summary>
        public string WebTemplateVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }
    }
}