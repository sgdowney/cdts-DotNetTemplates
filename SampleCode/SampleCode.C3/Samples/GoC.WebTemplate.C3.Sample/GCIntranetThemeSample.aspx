﻿<%@ Page Title="" Language="C#" MasterPageFile="~/GoC.WebTemplate/GoCWebTemplate.Master" AutoEventWireup="true" CodeBehind="GCIntranetThemeSample.aspx.cs" Inherits="GCIntranetThemeSample" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<h1>GoC Web Template Samples - GCIntranet Theme</h1>
<p><a href="http://www.gcpedia.gc.ca/wiki/Content_Delivery_Network/GoC_.NET_template_guide">Web Template Documentation (GCPedia)</a></p>

<p>This sample page provides the basic items to configure when required to use the GCIntranet theme.</p>

<h2>SubTheme</h2>
<p> The "SubTheme" is an optional parameter. Blank will render the default theme.  The only possible value at this time is "esdc", which is the internal theme for the esdc department.  It consists of a different color scheme and menu items.
    If you wish to use the subTheme, set the web.config <code class="wb-prettify">"GoC.WebTemplate.subTheme"</code> to "esdc". It can also be set programmatically via <code class="wb-prettify">"WebTemplateCore.WebTemplateSubTheme"</code>.
</p>

<h2>CDNEnvironment</h2>
<p>Set the web.config <code class="wb-prettify">"GoC.WebTemplate.environment"</code> to the environment of choice (other than Akamai) example "ESDC_PROD". It can also be set programmatically via <code class="wb-prettify">"WebTemplateCore.CDNEnvironment"</code>.</p>

<h2>Application Title</h2>
<p>When using the "GCIntranet" theme in your application, you can optionally set the Application Title text and its url. The application title is displayed at the top of the page, above the menu.</p>
<p>Set programmatically via the <code class="wb-prettify">"WebTemplateCore.ApplicationTitle.Text"</code>.
<p>The URL for the Application Title is optional.  If left blank the subTheme will determine the URL to use.  You can override the default URL value of the subTheme by setting programmatically the <code class="wb-prettify">"WebTemplateCore.ApplicationTitle.URL"</code> property of the Web Template.</p>

<div class="wb-prettify all-pre lang-vb linenums">
    <h3>Web Config Sample</h3>
    <pre>
&lt;GoC.WebTemplate 
        subTheme="esdc" 
        environment="ESDC_PROD" 
        </pre>
</div>

<div class="wb-prettify all-pre lang-vb linenums">
    <h3>C# Code Sample</h3>
    <pre>
this.WebTemplateCore.WebTemplateSubTheme = "esdc";
this.WebTemplateCore.CDNEnvironment = "ESDC_PROD";
this.WebTemplateCore.ApplicationTitle.Text = "My Custom Title";
this.WebTemplateCore.ApplicationTitle.URL = "http://iservice.prv/eng/index.shtml";           
        </pre>
</div>
   <!-- #include virtual="SamplesNavigation.html" -->
</asp:Content>
