﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net;
using System.Web;
using System.Linq;
using System.Threading;
using GoC.WebTemplate.Proxies;
using WebTemplateCore.JSONSerializationObjects;
using WebTemplateCore.Proxies;

// ReSharper disable once CheckNamespace
namespace GoC.WebTemplate
{
    public class Core
    {

        private readonly ICacheProxy _cacheProxy;
        private readonly IConfigurationProxy _configProxy;
        private readonly IDictionary<string,ICDTSEnvironment> _cdtsEnvironments;

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Enum that represents the social sites to be displayed when the user clicks the "Share this Page" link.
        /// The list of accepted sites can be found here: http://wet-boew.github.io/v4.0-ci/docs/ref/share/share-en.html
        /// </summary>
        /// <remarks>The enum item name must match the name expected by the Closure Template for the template to work.  The enum item name is used by the Closure Template to retreive the url, image, text for that social site</remarks>
        public enum SocialMediaSites
        {
            bitly,
            blogger,
            delicious,
            digg,
            diigo,
            email,
            facebook,
            gmail,
            googleplus,
            linkedin,
            myspace,
            pinterest,
            reddit,
            stumbleupon,
            tumblr,
            twitter,
            yahoomail
        }; //NOTE: The item names must match the parameter names expected by the Closure Template for this to work
        // ReSharper restore InconsistentNaming

        public Core(ICurrentRequestProxy currentRequest,
            ICacheProxy cacheProxy,
            IConfigurationProxy configProxy,
            IDictionary<string,ICDTSEnvironment> cdtsEnvironments)
        {
            _cacheProxy = cacheProxy;
            _configProxy = configProxy;
            _cdtsEnvironments = cdtsEnvironments;


            SetDefaultValues(currentRequest);
        }

        private void SetDefaultValues(ICurrentRequestProxy currentRequest)
        {
//Set properties
            WebTemplateVersion = _configProxy.Version;

            UseHTTPS = _configProxy.UseHttps;
            //Normalizing to match with the value we read from the configuration file.
            Environment = _configProxy.Environment.ToUpper();

            LoadJQueryFromGoogle = _configProxy.LoadJQueryFromGoogle;

            SessionTimeout = new SessionTimeout
            {
                Enabled = _configProxy.SessionTimeOut.Enabled,
                Inactivity = _configProxy.SessionTimeOut.Inactivity,
                ReactionTime = _configProxy.SessionTimeOut.ReactionTime,
                SessionAlive = _configProxy.SessionTimeOut.Sessionalive,
                LogoutUrl = _configProxy.SessionTimeOut.Logouturl,
                RefreshCallbackUrl = _configProxy.SessionTimeOut.RefreshCallbackUrl,
                RefreshOnClick = _configProxy.SessionTimeOut.RefreshOnClick,
                RefreshLimit = _configProxy.SessionTimeOut.RefreshLimit,
                Method = _configProxy.SessionTimeOut.Method,
                AdditionalData = _configProxy.SessionTimeOut.AdditionalData
            };

            //Set Top section options
            LanguageLink = new LanguageLink
            {
                Href = CoreBuilder.BuildLanguageLinkURL(currentRequest.QueryString)
            };
            ShowPreContent = _configProxy.ShowPreContent;
            ShowSearch = _configProxy.ShowShearch;

            //Set preFooter section options
            ShowPostContent = _configProxy.ShowPostContent;
            ShowFeedbackLink = _configProxy.ShowFeedbackLink;
            FeedbackLinkURL = _configProxy.FeedbackLinkurl;
            ShowLanguageLink = _configProxy.ShowLanguageLink;
            ShowSharePageLink = _configProxy.ShowSharePageLink;

            //Set Footer section options
            ShowFeatures = _configProxy.ShowFeatures;

            LeavingSecureSiteWarning = new LeavingSecureSiteWarning
            {
                Enabled = _configProxy.LeavingSecureSiteWarning.Enabled,
                DisplayModalWindow = _configProxy.LeavingSecureSiteWarning.DisplayModalWindow,
                RedirectURL = _configProxy.LeavingSecureSiteWarning.RedirectURL,
                ExcludedDomains = _configProxy.LeavingSecureSiteWarning.ExcludedDomains
            };

            //Set Application Template Specific Sections
            SignOutLinkURL = _configProxy.SignOutLinkURL;
            SignInLinkURL = _configProxy.SignInLinkURL;
            CustomSearch = _configProxy.CustomSearch;
        }

        public LeavingSecureSiteWarning LeavingSecureSiteWarning { get; set; }

        private string _staticFilesPath;

        /// <summary>
        /// property to hold the version of the template. it will be put as a comment in the html of the master pages. this will help us troubleshoot issues with clients using the template
        /// </summary>
        public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Represents the Application Title setting information
        /// Set Programmatically
        /// </summary>
        /// <remarks>Usable in Intranet Themes and Application Template</remarks>
        public Link ApplicationTitle { get; } = new Link();

        /// <summary>
        /// Represents the list of links for the Breadcrumbs
        /// Set by application programmatically
        /// </summary>
        public List<Breadcrumb> Breadcrumbs { get; set; } = new List<Breadcrumb>();


        /// <summary>
        /// The environment to use (akamai, ESDCPRod, ESDCNonProd)
        /// The environment provided will determine the CDTS that will be used (url and cdnenv)
        /// Set by application via the web.config or programmatically
        /// </summary>
        public string Environment { get; set; }

        public ICDTSEnvironment CurrentEnvironment => _cdtsEnvironments[Environment];
        /// <summary>
        /// CDNEnv from the cdtsEnvironments node of the web.config, for the specified environment
        /// Set by application via web.config
        /// </summary>
         public string CDNEnvironment => CurrentEnvironment.CDN;

        /// <summary>
        /// The local path to be used during local testing or perfomance testing
        /// Set by application via web.config
        /// </summary>
        public string LocalPath => CurrentEnvironment.LocalPath;

        /// <summary>
        /// URL from the cdtsEnvironments node of the web.config, for the specified environment
        /// Set by application via web.config
        /// </summary>
        public string CDNURL => CurrentEnvironment.Path;

        /// <summary>
        /// Represents the Theme to use to build the age. ex: GCWeb
        /// Set by application via web.config or programmatically
        /// </summary>
        public string WebTemplateTheme => CurrentEnvironment.Theme;

        /// <summary>
        /// Complete path of the CDN including http(s), theme and run or versioned
        /// Set by Core
        /// </summary>
        public string CDNPath => BuildCDNPath();

        /// <summary>
        /// Text to append to HeaderTitle
        /// </summary>
        public string AppendToTitle => CurrentEnvironment.AppendToTitle;

        /// <summary>
        /// Used to override the Contact link in Footer, AppFooter and TransacationalFooter
        /// Set by application programmatically
        /// </summary>
        public Link ContactLink { get; set; }

        private List<Link> BuildContactLinks()
        {
            if (string.IsNullOrWhiteSpace(ContactLink?.Href))
            {
                return null;
            }
            return  new List<Link> {ContactLink};
        }

        /// <summary>
        /// Represents the list of html elements to add to the header tag
        /// will be used to add metatags, css, js etc.
        /// Set by application programmatically
        /// </summary>
        public List<string> HTMLHeaderElements { get; set; } = new List<string>();

        /// <summary>
        /// Represents the list of html elements to add at the end of the body tag
        /// will be used to add metatags, css, js etc.
        /// Set by application programmatically
        /// </summary>
        public List<string> HTMLBodyElements { get; set; } = new List<string>();

        /// <summary>
        /// Represents the date modified displayed just above the footer
        /// Set by application programmatically
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Determines if the features of the footer are to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowFeatures { get; set; }

        /// <summary>
        /// URL to be used for the feedback link
        /// Set by application via web.config
        /// or programmatically
        /// </summary>
        public string FeedbackLinkURL { get; set; }

        /// <summary>
        /// URL to be used for the Privacy link in transactional mode
        /// Set by application programmatically
        /// </summary>
        public string PrivacyLinkURL { get; set; }

        /// <summary>
        /// URL to be used for the Terms & Conditions link in transactional mode
        /// Set by application programmatically
        /// </summary>
        public string TermsConditionsLinkURL { get; set; }

        /// <summary>
        /// Used to override the langauge link
        /// </summary>
        public LanguageLink LanguageLink { get; set; }

        // ReSharper restore InconsistentNaming
        /// <summary>
        /// Represents a list of menu items
        /// </summary>
        public List<MenuSection> LeftMenuItems { get; set; } = new List<MenuSection>();

        /// <summary>
        /// A unique string to identify a web page. Used by user to identify the screen where an issue occured.
        /// </summary>
        public string ScreenIdentifier { get; set; }

        /// <summary>
        /// Defines the session timeout properties
        /// The objects properties are set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public SessionTimeout SessionTimeout { get; set; }

        /// <summary>
        /// Determines if the Post Content of the footer are to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowPostContent { get; set; }

        /// <summary>
        /// Determines if the FeedBack link of the footer is to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowFeedbackLink { get; set; }

        /// <summary>
        /// Determines if the language toggle link  is to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowLanguageLink { get; set; }

        /// <summary>
        /// Determines if the Share Link of the footer are to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowSharePageLink { get; set; }

        /// <summary>
        /// Determines if the Search control of the header are to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowSearch { get; set; }

        /// <summary>
        /// Representes the list of items to be displayed in the Share Page window
        /// Set by application programmatically
        /// </summary>
        public List<SocialMediaSites> SharePageMediaSites { get; set; } = new List<SocialMediaSites>();

        /// <summary>
        /// Determines the path to the location of the staticback up files
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        /// <remarks>The "theme" is concatenated to the end of this path.</remarks>
        public string StaticFilesPath
        {
            get
            {
                return _staticFilesPath ??
                       (_staticFilesPath = string.Concat(_configProxy.StaticFilesLocation, "/", WebTemplateTheme));
            }
            set { _staticFilesPath = value; }
        }

        /// <summary>
        /// Determines if the Pre Content of the header are to be displayed
        /// Set by application via web.config
        /// or Set by application programmatically
        /// </summary>
        public bool ShowPreContent { get; set; }

        /// <summary>
        /// Retreive the first 2 letters of the current culture "en" or "fr"
        /// Used by generate paths, determine language etc...
        /// Set by Template
        /// </summary>
        public string TwoLetterCultureLanguage => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        private string _headerTitle;
        /// <summary>
        /// title of page, will automatically add '- Canada.ca' to all pages implementing GCWeb theme as per
        /// Set by application programmatically
        /// </summary>
        public string HeaderTitle
        {
            get
            {

                if (string.IsNullOrWhiteSpace(AppendToTitle))
                {
                    return _headerTitle;
                }

                if (string.IsNullOrWhiteSpace(_headerTitle))
                {
                    return AppendToTitle;
                }

                if (_headerTitle.EndsWith(AppendToTitle))
                {
                    return _headerTitle;
                }

                return _headerTitle + AppendToTitle;
            }
            set
            {
                _headerTitle = value;
            }
        }

        /// <summary>
        /// version of application to be displayed instead of the date modified
        /// set by application programmatically
        /// </summary>
        public string VersionIdentifier { get; set; }

        /// <summary>
        /// Represents the Version of the CDN files to use to build the page. ex 4.0.17
        /// Set by application via web.config or programmatically
        /// </summary>
        public string WebTemplateVersion { get; set; }



        /// <summary>
        /// Represents the sub Theme to use to build the age. ex: esdc
        /// </summary>
        public string WebTemplateSubTheme => _cdtsEnvironments[Environment].SubTheme;

        /// <summary>
        /// Determines if the communication between the browser and the CDTS should be encrypted
        /// Set by application via web.config or programmatically
        /// </summary>
        public bool? UseHTTPS { get; set; }

        /// <summary>
        /// Determines if the jQuery files should be loaded from google or from the CDN
        /// Set by application via web.config or programmatically
        /// </summary>
        public bool LoadJQueryFromGoogle { get; set; }



        /// <summary>
        /// Allows for a custom search to be used in the application, you must contact CDTS to have one created.
        /// </summary>
        public string CustomSearch { get; set; }

        /// <summary>
        /// A custom site menu to be used in place of the standard canada.ca site menu
        /// This defaults to null (use standard menu)
        /// Set by application programmatically or in the Web.Config
        /// Only available in the Application Template
        /// </summary>
        public string CustomSiteMenuURL { get; set; }

        /// <summary>
        /// The link to use for the App Settings in the AppTop
        /// Set by application programmatically or in the Web.Config
        /// Only available in the Application Template
        /// </summary>
        public string AppSettingsURL { get; set; }

        /// <summary>
        /// The link to use for the sign in button, will only appear if <see cref="ShowSignInLink"/> is set to true
        /// Set by application programmatically or in the Web.Config
        /// Only available in the Application Template
        /// </summary>
        public string SignInLinkURL { get; set; }

        /// <summary>
        /// The link to use for the sign out button, will only appear if <see cref="ShowSignOutLink"/> is set to true
        /// Set by application programmatically or in the Web.Config
        /// Only available in the Application Template
        /// </summary>
        public string SignOutLinkURL { get; set; }


        /// <summary>
        /// Displays the sign in link set.
        /// <see cref="SignInLinkURL"/> must not be null or whitespace
        /// <see cref="ShowSignOutLink"/> must not be set at the same time.
        /// Set by application programmatically
        /// Only available in the Application Template
        /// </summary>
        public bool ShowSignInLink { get; set; }

        /// <summary>
        /// Displays the signout link set.
        /// <see cref="SignOutLinkURL"/> must not be null or whitespace
        /// <see cref="ShowSignInLink"/> must not be set at the same time.
        /// Set by application programmatically
        /// Only available in the Application Template
        /// </summary>
        public bool ShowSignOutLink { get; set; }

        /// <summary>
        /// Custom links if null uses standard links if not null overrides the existing footer links
        /// Set by application programmatically
        /// Only available in the Application Template
        /// </summary>
        public List<FooterLink> CustomFooterLinks { get; set; }

        /// <summary>
        /// Custom Links for the top Menu added for MSCAs (Currently) use only. 
        /// Set by application programmatically
        /// Only available in the Application Template
        /// </summary>
        public List<MenuLink> MenuLinks { get; set; }
        
        private string BuildLocalPath()
        {
            return GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion);
        }

        public List<Breadcrumb> BuildBreadcrumbs()
        {
            if (Breadcrumbs == null || !Breadcrumbs.Any())
            {
                return null;
            }

            return Breadcrumbs.Select(b => new Breadcrumb
            {
                Href = GetStringForJson(b.Href),
                Acronym = GetStringForJson(b.Acronym),
                Title = GetStringForJson(b.Title)
            }).ToList();
        }
        public List<FooterLink> BuildCustomFooterLinks
        {
            get
            {
                return CustomFooterLinks?.Select(fl => new FooterLink
                {
                    Href = fl.Href,
                    NewWindow = fl.NewWindow,
                    Text = GetStringForJson(fl.Text)
                }).ToList();
            }
        }

        private string GetStringForJson(string str) => string.IsNullOrWhiteSpace(str) ? null : str;

        private string GetFormattedJsonString(string formatStr, params object[] strs) => string.IsNullOrWhiteSpace(formatStr) ? null : string.Format(formatStr, strs);

        private List<Link> BuildHideableHrefOnlyLink(string href, bool showLink)
        {

            if (!showLink || string.IsNullOrWhiteSpace(href))
            {
                return null;
            }
            return new List<Link> {new Link {Href = href, Text = null}};
        }

        private void CheckIfBothSignInAndSignOutAreSet()
        {
            if (ShowSignInLink && ShowSignOutLink)
            {
                throw new InvalidOperationException("Unable to show sign in and sign out link together");
            }
        }

        public HtmlString RenderHeaderTitle() => new HtmlString(HeaderTitle);

        public HtmlString RenderAppFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new AppFooter
            {
                CdnEnv = CDNEnvironment,
                SubTheme = GetStringForJson(WebTemplateSubTheme),
                ShowFeatures = ShowFeatures,
                TermsLink = GetStringForJson(TermsConditionsLinkURL),
                PrivacyLink = GetStringForJson(PrivacyLinkURL),
                ContactLink = GetStringForJson(ContactLink?.Href),
                LocalPath = GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion),
                FooterSections = BuildCustomFooterLinks
            });
        }

        public HtmlString RenderAppTop()
        {
            CheckIfBothSignInAndSignOutAreSet();
            CheckIfCustomMenuURLAndMenuLinkAreBothSet();

            //For v4.0.26.x we have to render this section differently depending on the theme, 
            //GCIntranet theme renders AppName and AppUrl seperately in GCWeb we render it as a List of Links. 
            return WebTemplateTheme.ToLower() == "gcweb" ? RenderGCWebAppTop() : RenderGCIntranetApptop();
        }

        private void CheckIfCustomMenuURLAndMenuLinkAreBothSet()
        {
            if (CustomSiteMenuURL != null && MenuLinks != null && MenuLinks.Any())
            {
                throw new InvalidOperationException("Unable to have both a custom menu url and dynamically generated menu at the same time");
            }
        }

        private HtmlString RenderGCIntranetApptop()
        {
            return JsonSerializationHelper.SerializeToJson(new GCIntranetAppTop
            {
                AppName = new List<Link> {ApplicationTitle},
                IntranetTitle = BuildIntranentTitleList(),
                SignIn = BuildHideableHrefOnlyLink(SignInLinkURL, ShowSignInLink),
                SignOut = BuildHideableHrefOnlyLink(SignOutLinkURL, ShowSignOutLink),
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                Search = ShowSearch,
                LngLinks = BuildLanguageLinkList(),
                ShowPreContent = ShowPreContent,
                Breadcrumbs = BuildBreadcrumbs(),
                LocalPath = GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion),
                AppSettings = BuildHideableHrefOnlyLink(AppSettingsURL, true),
                MenuPath = CustomSiteMenuURL,
                CustomSearch = CustomSearch,
                TopSecMenu = LeftMenuItems.Any(),
                MenuLinks = MenuLinks
            });
        }

        private HtmlString RenderGCWebAppTop()
        {
            return JsonSerializationHelper.SerializeToJson(new AppTop
            {
                AppName = new List<Link> {ApplicationTitle},
                SignIn = BuildHideableHrefOnlyLink(SignInLinkURL, ShowSignInLink),
                SignOut = BuildHideableHrefOnlyLink(SignOutLinkURL, ShowSignOutLink),
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                Search = ShowSearch,
                LngLinks = BuildLanguageLinkList(),
                ShowPreContent = ShowPreContent,
                Breadcrumbs = BuildBreadcrumbs(),
                LocalPath = GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion),
                AppSettings = BuildHideableHrefOnlyLink(AppSettingsURL, true),
                MenuPath = CustomSiteMenuURL,
                CustomSearch = CustomSearch,
                TopSecMenu = LeftMenuItems.Any(),
                MenuLinks = MenuLinks
            });
        }

        public Link IntranetTitle { get; set; }

        public HtmlString RenderTransactionalTop()
        {
            return JsonSerializationHelper.SerializeToJson(new Top
            {

                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                IntranetTitle = BuildIntranentTitleList(),
                Search = ShowSearch,
                LngLinks = BuildLanguageLinkList(),
                Breadcrumbs = BuildBreadcrumbs(),
                ShowPreContent = false,
                LocalPath = BuildLocalPath(),
                TopSecMenu = LeftMenuItems.Any(), 
                SiteMenu = false

            });
        }

        public HtmlString RenderTop()
        {
            return JsonSerializationHelper.SerializeToJson(new Top
            {
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                IntranetTitle = BuildIntranentTitleList(),
                Search = ShowSearch,
                LngLinks = BuildLanguageLinkList(),
                ShowPreContent = ShowPreContent,
                Breadcrumbs = BuildBreadcrumbs(),
                LocalPath = BuildLocalPath(),
                TopSecMenu = LeftMenuItems.Any(),
                SiteMenu = true
            });
        }

        public HtmlString RenderRefTop(bool isApplication)
        {
            return JsonSerializationHelper.SerializeToJson(new RefTop
            {
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                JqueryEnv = LoadJQueryFromGoogle ? "external" : null,
                LocalPath = BuildLocalPath(),
                IsApplication = isApplication
            });
        }

        public HtmlString RenderUnilingualPreFooter() {
            return JsonSerializationHelper.SerializeToJson(new UnilingualPreFooter
            {
                CdnEnv = CDNEnvironment,
                PageDetails = false
            });
        }

        public HtmlString RenderPreFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new PreFooter
            {
                CdnEnv = CDNEnvironment,
                DateModified = BuildDateModified(),
                VersionIdentifier = GetStringForJson(VersionIdentifier),
                ShowPostContent = ShowPostContent,
                ShowFeedback = new FeedbackLink
                {
                    Show = ShowFeedbackLink,
                    URL = FeedbackLinkURL
                },
                ShowShare = new ShareList
                {
                    Show = ShowSharePageLink,
                    Enums = SharePageMediaSites
                },
                ScreenIdentifier = GetStringForJson(ScreenIdentifier)
            });
        }

        public HtmlString RenderTransactionalPreFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new PreFooter
            {
                CdnEnv = CDNEnvironment,
                DateModified = BuildDateModified(),
                VersionIdentifier = GetStringForJson(VersionIdentifier),
                ShowPostContent = false,
                ShowFeedback = new FeedbackLink {Show = false},
                ShowShare = new ShareList { Show = false},
                ScreenIdentifier = GetStringForJson(ScreenIdentifier)
            });
        }

        public HtmlString RenderFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new Footer
            {
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                ShowFeatures = ShowFeatures,
                ShowFooter = true,
                ContactLinks = BuildContactLinks(),
                PrivacyLink = null,
                TermsLink = null

            });
        }

        public HtmlString RenderTransactionalFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new Footer
            {
                CdnEnv = CDNEnvironment,
                SubTheme = WebTemplateSubTheme,
                ShowFeatures = ShowFeatures,
                ShowFooter = false,
                ContactLinks = BuildContactLinks(),
                PrivacyLink = GetStringForJson(PrivacyLinkURL),
                TermsLink = GetStringForJson(TermsConditionsLinkURL)

            });

        }

        public HtmlString RenderRefFooter()
        {
            if (LeavingSecureSiteWarning.Enabled && 
                !string.IsNullOrEmpty(LeavingSecureSiteWarning.RedirectURL))
            {
                return RenderSecureSiteWarningRefFooter();
            }
            return JsonSerializationHelper.SerializeToJson(new RefFooter
            {
                CdnEnv = CDNEnvironment,
                ExitScript = false,
                JqueryEnv = BuildJqueryEnv(), 
                LocalPath = GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion)
            });
        }

        private HtmlString RenderCDNEnvOnly() => JsonSerializationHelper.SerializeToJson(new CDNEnvOnly {CdnEnv = CDNEnvironment});

        public HtmlString RenderServerTop() => RenderCDNEnvOnly();
        public HtmlString RenderServerBottom() => RenderCDNEnvOnly();
        public HtmlString RenderServerRefTop() => RenderCDNEnvOnly();
        public HtmlString RenderServerRefFooter() => RenderCDNEnvOnly();

        private string BuildJqueryEnv() => LoadJQueryFromGoogle ? "external" : null;

        private HtmlString RenderSecureSiteWarningRefFooter()
        {
            return JsonSerializationHelper.SerializeToJson(new RefFooter
            {
                CdnEnv = CDNEnvironment,
                ExitScript = true,
                DisplayModal = LeavingSecureSiteWarning.DisplayModalWindow,
                ExitURL = LeavingSecureSiteWarning.RedirectURL,
                ExitMsg = WebUtility.HtmlEncode(LeavingSecureSiteWarning.Message),
                ExitDomains = GetStringForJson(LeavingSecureSiteWarning.ExcludedDomains),
                JqueryEnv = BuildJqueryEnv(),
                LocalPath= GetFormattedJsonString(LocalPath, WebTemplateTheme, WebTemplateVersion)
            });

        }

        
        private string BuildDateModified()
        {

            if (DateTime.Compare(DateModified, DateTime.MinValue) == 0)
            {
                return null;
            }
            return DateModified.ToString("yyyy-MM-dd");
        }

        private List<Link> BuildIntranentTitleList() => IntranetTitle == null ? null : new List<Link> { IntranetTitle };

        private List<LanguageLink> BuildLanguageLinkList()
        {
            if (!ShowLanguageLink)
            {
                return null;
            }

            return new List<LanguageLink> {
                new LanguageLink {
                    Href = LanguageLink.Href
                }
            };
        }

        /// <summary>
        /// Builds the html of the WET Session Timeout control that provides session timeout and inactivity functionality.
        /// For more documentation: https://wet-boew.github.io/v4.0-ci/demos/session-timeout/session-timeout-en.html
        /// </summary>
        /// <returns>The html of the WET session timeout control
        /// </returns>
        public HtmlString RenderSessionTimeoutControl()
        {
            StringBuilder sb = new StringBuilder();
            //<span class='wb-sessto' data-wb-sessto='{"inactivity": 5000, "reactionTime": 30000, "sessionalive": 10000, "logouturl": "http://www.tsn.com", "refreshCallbackUrl": "http://www.cnn.com", "refreshOnClick": "33", "refreshLimit": 2, "method": "555", "additionalData": "666"}'></span>

            if (SessionTimeout.Enabled)
            {
                sb.Append("<span class='wb-sessto' data-wb-sessto='{");
                sb.Append("\"inactivity\": ");
                sb.Append(SessionTimeout.Inactivity);
                sb.Append(", \"reactionTime\": ");
                sb.Append(SessionTimeout.ReactionTime);
                sb.Append(", \"sessionalive\": ");
                sb.Append(SessionTimeout.SessionAlive);
                sb.Append(", \"logouturl\": \"");
                sb.Append(SessionTimeout.LogoutUrl);
                sb.Append("\"");
                if (!string.IsNullOrEmpty(SessionTimeout.RefreshCallbackUrl))
                {
                    sb.Append(", \"refreshCallbackUrl\": \"");
                    sb.Append(SessionTimeout.RefreshCallbackUrl);
                    sb.Append("\"");
                }
                sb.Append(", \"refreshOnClick\": ");
                sb.Append(SessionTimeout.RefreshOnClick.ToString().ToLower());

                if (SessionTimeout.RefreshLimit > 0)
                {
                    sb.Append(", \"refreshLimit\": ");
                    sb.Append(SessionTimeout.RefreshLimit);
                }
                if (!string.IsNullOrEmpty(SessionTimeout.Method))
                {
                    sb.Append(", \"method\": \"");
                    sb.Append(SessionTimeout.Method);
                    sb.Append("\"");
                }
                if (!string.IsNullOrEmpty(SessionTimeout.AdditionalData))
                {
                    sb.Append(", \"additionalData\": \"");
                    sb.Append(SessionTimeout.AdditionalData);
                    sb.Append("\"");
                }
                sb.Append("}'></span>");
            }
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Builds a string with the format required by the closure templates, to represent the left side menu
        /// </summary>
        /// <returns>
        /// string in the format expected by the Closure Templates to generate the left menu
        /// </returns>
        public HtmlString RenderLeftMenu()
        {
            StringBuilder sb = new StringBuilder();

            // sectionName: 'Section menu', menuLinks: [{ href: '#', text: 'Link 1' }, { href: '#', text: 'Link 2' }]"

            if (LeftMenuItems.Count > 0)
            {
                sb.Append("sections: [");
                foreach (MenuSection menuSection in LeftMenuItems)
                {
                    //add section name
                    sb.Append(" {sectionName: '");
                    sb.Append(WebUtility.HtmlEncode(menuSection.Name));
                    sb.Append("',");

                    //add section link
                    if (!string.IsNullOrEmpty(menuSection.Link))
                    {
                        sb.Append(" sectionLink: '");
                        sb.Append(WebUtility.HtmlEncode(menuSection.Link));
                        sb.Append("',");
                        if (menuSection.OpenInNewWindow)
                        {
                            sb.Append("newWindow: true,");
                        }
                    }

                    //add menu items
                    if (menuSection.Items.Count > 0)
                    {
                        sb.Append(" menuLinks: [");
                        foreach (Link lk in menuSection.Items)
                        {
                            sb.Append("{href: '");
                            sb.Append(lk.Href);
                            sb.Append("', text: '");
                            sb.Append(WebUtility.HtmlEncode(lk.Text));
                            sb.Append("'");

                            //Add 3rd level sub items, Note: Template is limiting to 3 levels even if Core allows more
                            if (lk is MenuItem)
                            {
                                MenuItem mi = (MenuItem) lk;

                                //the following if statement needs to be here for backward compatibility OpenInNewWindow is only available to MenuItems not Link
                                if (mi.OpenInNewWindow)
                                {
                                    sb.Append(", newWindow: true");
                                }

                                if (mi.SubItems.Count > 0)
                                {
                                    sb.Append(", subLinks: [");
                                    foreach (MenuItem sublk in mi.SubItems)
                                    {
                                        sb.Append("{subhref: '");
                                        sb.Append(sublk.Href);
                                        sb.Append("', subtext: '");
                                        sb.Append(WebUtility.HtmlEncode(sublk.Text));
                                        sb.Append("',");
                                        if (sublk.OpenInNewWindow)
                                        {
                                            sb.Append(" newWindow: true");
                                        }
                                        sb.Append("},");
                                    }
                                    sb.Append("]");
                                }
                            }
                            sb.Append("},");
                        }
                        sb.Append("]");
                    }
                    sb.Append("},");
                }
                sb.Append("]");
            }
            return new HtmlString(sb.ToString());
        }

        public HtmlString RenderHtmlHeaderElements() => RenderHtmlElements(HTMLHeaderElements);

        public HtmlString RenderHtmlBodyElements() => RenderHtmlElements(HTMLBodyElements);

        /// <summary>
        /// Adds a string(html tag) to be included in the page
        /// This is our way off letting the developer add metatags, css and js to their pages.
        /// </summary>
        /// <remarks>we are accepting a string with no validation, therefore it is up to the developer to provide a valid string/html tag</remarks>
        /// <returns>
        /// string hopefully a valid html tag
        /// </returns>
        private HtmlString RenderHtmlElements(List<string> tags)
        {
            var sb = new StringBuilder();

            foreach (var tag in tags)
            {
                sb.AppendLine(tag);
            }
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Builds the path to the cdn based on the environment set in the config. The path is based on the url of the environment, theme and version
        /// </summary>
        /// <returns>String, the complete path to the cdn</returns>
        private string BuildCDNPath()
        {

            var currentEnv = _cdtsEnvironments[Environment];


            if (!currentEnv.IsEncryptionModifiable && UseHTTPS.HasValue)
            {
                throw new InvalidOperationException($"{Environment} does not allow useHTTPS to be toggled");
            }

            if (currentEnv.IsEncryptionModifiable && !UseHTTPS.HasValue)
            {
                throw new InvalidOperationException($"{Environment} requires useHTTPS to be true or false not null.");
            }

            var https = string.Empty;
            if (currentEnv.IsEncryptionModifiable)
            {
                //We've already checked to see if this is null before here so ignore this in resharper
                // ReSharper disable once PossibleInvalidOperationException
                https = UseHTTPS.Value ? "s" : string.Empty;
            }


            var run = string.Empty;
            var version = string.Empty;
            if (string.IsNullOrWhiteSpace(WebTemplateVersion))
            {
                if (currentEnv.IsVersionRNCombined)
                {
                    version = "rn/";
                }
                else
                {
                    run = "rn";
                }
            }
            else
            {
                version = WebTemplateVersion + "/";
                run = "app";
            }


            return string.Format(CultureInfo.InvariantCulture, currentEnv.Path, https, run, WebTemplateTheme, version);
        }

        /// <summary>
        /// Arbritrary object to act as a mutex to obtain a class-scope lock accros all threads.
        /// </summary>
        /// <remarks></remarks>
        private static readonly object LockObject = new object();

        /// <summary>
        /// This method is used to get the static file content from the cache. if the cache is empty it will read the content from the file and load it into the cache.
        /// </summary>
        /// <param name="fileName">static file name to retreive</param>
        /// <returns>A string containing the content of the file.</returns>
        public HtmlString LoadStaticFile(string fileName)
        {
            var cacheKey = string.Concat(Constants.CACHE_KEY_STATICFILES_PREFIX, ".", fileName);

            // Attempt to lookup from cache
            Debug.Assert(_cacheProxy != null, "Cache proxy cannot be null");
            // ReSharper disable once InconsistentlySynchronizedField
            var info = _cacheProxy.GetFromCache<string>(cacheKey);
            if (info != null)
            {
                // Object was found in cache, simply return it and get out!
                return new HtmlString(info);
            }

            // ---[ If we get here, the object was not found in the cache, we'll have to load it.
            lock (LockObject)
            {
                //---[ Attempt to get from cache again now that we are locked
                info = _cacheProxy.GetFromCache<string>(cacheKey);
                if (info != null)
                {
                    // Once again, object was found in cache, simply return it and get out!
                    return new HtmlString(info);
                }

                //---[ If we get here, we really have to load the data
                string filePath;
                if (StaticFilesPath.StartsWith("~"))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    filePath = Path.Combine(HttpContext.Current.Server.MapPath(StaticFilesPath),
                        fileName);
                }
                else
                {
                    filePath = Path.Combine(StaticFilesPath, fileName);
                }

                try
                {
                    info = File.ReadAllText(filePath);
                    //---[ Now that the data is loaded, add it to the cache
                    _cacheProxy.SaveToCache(cacheKey, filePath, info);
                }
                catch (DirectoryNotFoundException)
                {
                    info = string.Empty;
                }
                catch (FileNotFoundException)
                {
                    info = string.Empty;
                }
            }
            return new HtmlString(info);
        }


    }

    public class CDNEnvOnly
    {
        public string CdnEnv { get; set; }
    }
}



