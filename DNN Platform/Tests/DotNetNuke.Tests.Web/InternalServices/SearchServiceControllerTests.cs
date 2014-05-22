﻿#region Copyright
//
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Http;
using DotNetNuke.ComponentModel;
using DotNetNuke.Data;
using DotNetNuke.Entities.Portals.Data;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.Search.Internals;
using DotNetNuke.Tests.Utilities.Mocks;
using Moq;

using NUnit.Framework;
using Constants = DotNetNuke.Services.Search.Internals.Constants;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Web.InternalServices;
using DotNetNuke.Web.InternalServices.Views.Search;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;

namespace DotNetNuke.Tests.Web.InternalServices
{
    /// <summary>
    ///  Testing grouping logic of GetGroupedBasicView and GetGroupedDetailView (SearchServiceController methods)
    /// </summary>
    [TestFixture]
    [AllowAnonymous]
    public class SearchServiceControllerTests
    {

        #region Constants
        private const int ModuleSearchTypeId = 1;
        private const int TabSearchTypeId = 2;
        private const int UserSearchTypeId = 3;
        private const int DocumentSearchTypeId = 4;
        private const int UrlSearchTypeId = 5;
        private const int OtherSearchTypeId = 6;
        private const int PortalId0 = 0;
        private const int PortalId12 = 12;
        private const int IdeasModuleDefId = 201;
        private const int BlogsoduleDefId = 202;
        private const int AnswersModuleDefId = 203;
        private const int HtmlModuleDefId = 20;
        private const int HtmlModuleId = 25;
        private const int IdeasModuleId = 301;
        private const int BlogsModuleId = 302;
        private const int AnswersModuleId = 303;
        private const int RoleId731 = 731;
        private const int RoleId532 = 532;
        private const int RoleId0 = 0;
        private const int HtmlModDefId = 116;
        private const int TabId1 = 56;
        private const string TabName1 = "Home";
        private const string TabNameUrl = "/Home.aspx";
        private const int HtmlModuleId1 = 367;
        private const int HtmlModuleId2 = 368;
        private const int HtmlModuleId3 = 370;
        private const string HtmlModuleTitle1 = "TitleWelcome";
        private const string HtmlModuleTitle2 = "TitleProducts";
        private const string HtmlModuleTitle3 = "TitleServices";
        private const int TabId2 = 57;
        private const string TabName2 = "About Us";
        private const string TabNameUrl2 = "/AboutUs.aspx";
        private const string HtmlModuleTitle4 = "TitleAboutUs";
        private const int HtmlModuleId4 = 378;
        private const int UserId1 = 1;
        private const string UserName1 = "User1";
        private const string Tag0 = "tag0";
        private const string Tag0WithSpace = "tag0 hello";
        private const string Tag1 = "tag1";
        private const string Tag2 = "tag2";
        private const string Tag3 = "tag3";
        private const string Tag4 = "tag4";
        private const string TagTootsie = "tootsie";
        private const string TagLatest = "latest";
        private const string TagOldest = "oldest";
        private const string TagIt = "IT";
        private const string TagNeutral = "Neutral";
        
        private const string ModuleSearchTypeName = "module";
        private const string UserSearchTypeName = "user";
        private const string OtherSearchTypeName = "other";
        private const string TabSearchTypeName = "tab";
        private const string DocumentSearchTypeName = "document";
        private const string UrlSearchTypeName = "url";

        private const string ModuleResultControllerClass = "DotNetNuke.Tests.Web.InternalServices.FakeResultController, DotNetNuke.Tests.Web";
        private const string FakeResultControllerClass = "DotNetNuke.Tests.Web.InternalServices.FakeResultController, DotNetNuke.Tests.Web";
        private const string NoPermissionFakeResultControllerClass = "DotNetNuke.Tests.Web.InternalServices.NoPermissionFakeResultController, DotNetNuke.Tests.Web";
        
        private const string CultureEnUs = "en-US";
        private const string CultureEnCa = "en-CA";
        private const string CultureItIt = "it-IT";
        private const string CultureEsEs = "es-ES";
        
        private const string SearchIndexFolder = @"App_Data\SearchTests";
        private readonly double _readerStaleTimeSpan = TimeSpan.FromMilliseconds(100).TotalSeconds;

        #endregion

        #region Private Properties
        private Mock<IHostController> _mockHostController;
        private Mock<CachingProvider> _mockCachingProvider;
        private Mock<DataProvider> _mockDataProvider;
        private Mock<ILocaleController> _mockLocaleController;
        private Mock<IDataService> _mockDataService;
        private Mock<IUserController> _mockUserController;
        private Mock<IModuleController> _mockModuleController;
        private Mock<ITabController> _mockTabController;
        private SearchServiceController _searchServiceController;
        private IInternalSearchController _internalSearchController;
        private InternalSearchControllerImpl _internalSearchControllerImpl;
        private LuceneControllerImpl _luceneController;
        #endregion

        #region Set Up
        [SetUp]
        public void SetUp()
        {
            //Arrange
            ComponentFactory.Container = new SimpleContainer();
            MockComponentProvider.ResetContainer();

            _mockDataProvider = MockComponentProvider.CreateDataProvider();
            _mockLocaleController = MockComponentProvider.CreateLocaleController();
            _mockCachingProvider = MockComponentProvider.CreateDataCacheProvider();
            _mockDataService = new Mock<IDataService>();
            _mockUserController = new Mock<IUserController>();
            _mockModuleController = new Mock<IModuleController>();
            _mockTabController = new Mock<ITabController>();
            _mockHostController = new Mock<IHostController>();

            SetupDataProvider();
            SetupHostController();
            SetupUserController();
            SetupPortalSettings();
            SetupModuleController();
            DeleteIndexFolder();
            
            TabController.SetTestableInstance(_mockTabController.Object);
            _internalSearchControllerImpl = new InternalSearchControllerImpl();
            InternalSearchController.SetTestableInstance(_internalSearchControllerImpl);
            _internalSearchController = InternalSearchController.Instance;

            //create instance of the SearchServiceController
            _searchServiceController = new SearchServiceController(HtmlModDefId);
            CreateNewLuceneControllerInstance();
        }
        [TearDown]
        public void TearDown()
        {
            _luceneController.Dispose();
            DeleteIndexFolder();

        }
        #endregion

        #region Private Methods
        private void CreateNewLuceneControllerInstance()
        {
            if (_luceneController != null)
            {
                LuceneController.ClearInstance();
                _luceneController.Dispose();
            }
            _luceneController = new LuceneControllerImpl();
            LuceneController.SetTestableInstance(_luceneController);
        }
        private  void SetupUserController()
        {

            _mockUserController.Setup(c => c.GetUserById(It.IsAny<int>(), It.IsAny<int>())).Returns(
            new UserInfo { UserID = UserId1, Username = UserName1, Profile = new UserProfile {} });
            UserController.SetTestableInstance(_mockUserController.Object);
        }
        private void SetupHostController()
        {
            _mockHostController.Setup(c => c.GetString(Constants.SearchIndexFolderKey, It.IsAny<string>())).Returns(
                SearchIndexFolder);
            _mockHostController.Setup(c => c.GetDouble(Constants.SearchReaderRefreshTimeKey, It.IsAny<double>())).
                Returns(_readerStaleTimeSpan);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchTitleBoostSetting, It.IsAny<int>())).Returns(
                Constants.DefaultSearchTitleBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchTagBoostSetting, It.IsAny<int>())).Returns(
                Constants.DefaultSearchTagBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchContentBoostSetting, It.IsAny<int>())).Returns(
                Constants.DefaultSearchKeywordBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchDescriptionBoostSetting, It.IsAny<int>())).
                Returns(Constants.DefaultSearchDescriptionBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchAuthorBoostSetting, It.IsAny<int>())).Returns(
                Constants.DefaultSearchAuthorBoost);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchMinLengthKey, It.IsAny<int>())).Returns(
                Constants.DefaultMinLen);
            _mockHostController.Setup(c => c.GetInteger(Constants.SearchMaxLengthKey, It.IsAny<int>())).Returns(
                Constants.DefaultMaxLen);
            HostController.RegisterInstance(_mockHostController.Object);
        }
        private void SetupDataProvider()
        {
            //Standard DataProvider Path for Logging
            _mockDataProvider.Setup(d => d.GetProviderPath()).Returns("");

            _mockDataProvider.Setup(d => d.GetPortals(It.IsAny<string>())).Returns<string>(GetPortalsCallBack);
            _mockDataProvider.Setup(d => d.GetSearchModules(It.IsAny<int>())).Returns(GetSearchModules);
            _mockDataProvider.Setup(d => d.GetModuleDefinitions()).Returns(GetModuleDefinitions);
            _mockDataProvider.Setup(d => d.GetAllSearchTypes()).Returns(GetAllSearchTypes);
            _mockDataProvider.Setup(d => d.GetUser(It.IsAny<int>(), It.IsAny<int>())).Returns(GetUser);
            _mockDataProvider.Setup(d => d.GetTabs(It.IsAny<int>())).Returns(GetTabs);
            _mockDataService.Setup(ds => ds.GetPortalGroups()).Returns(GetPortalGroups);
            
            DataService.RegisterInstance(_mockDataService.Object); 


        }
        private void SetupPortalSettings()
        {
            var mockPortalController = new Mock<IPortalController>();
            mockPortalController.Setup(x => x.GetPortal(It.IsAny<int>())).Returns(new PortalInfo { PortalID = PortalId0, PortalGroupID = -1, UserTabId = TabId1, });
            PortalController.SetTestableInstance(mockPortalController.Object);
        }
        private void SetupModuleController()
        {
            _mockModuleController.Setup(mc => mc.GetModule(It.Is<int>(m => m == HtmlModuleId1), It.Is<int>(p => p == PortalId0), false)).Returns(
    new ModuleInfo { ModuleID = HtmlModuleId1, ModuleDefID = HtmlModDefId, ModuleTitle = HtmlModuleTitle1 });
            _mockModuleController.Setup(mc => mc.GetModule(It.Is<int>(m => m == HtmlModuleId2), It.Is<int>(p => p == PortalId0), false)).Returns(
            new ModuleInfo { ModuleID = HtmlModuleId2, ModuleDefID = HtmlModDefId, ModuleTitle = HtmlModuleTitle2 });
            _mockModuleController.Setup(mc => mc.GetModule(It.Is<int>(m => m == HtmlModuleId3), It.Is<int>(p => p == PortalId0), false)).Returns(
            new ModuleInfo { ModuleID = HtmlModuleId3, ModuleDefID = HtmlModDefId, ModuleTitle = HtmlModuleTitle3 });

            _mockModuleController.Setup(mc => mc.GetModule(It.Is<int>(m => m == HtmlModuleId4), It.Is<int>(p => p == PortalId0), false)).Returns(
            new ModuleInfo { ModuleID = HtmlModuleId4, ModuleDefID = HtmlModDefId, ModuleTitle = HtmlModuleTitle4 });
            ModuleController.SetTestableInstance(_mockModuleController.Object);
        }
        private void DeleteIndexFolder()
        {
            try
            {
                if (Directory.Exists(SearchIndexFolder))
                    Directory.Delete(SearchIndexFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private IDataReader GetUser()
        {
            var table = new DataTable("Users");
            table.Columns.Add("UserID", typeof(int));
            table.Columns.Add("PortalId", typeof(int));
            table.Columns.Add("UserName", typeof(string));
            table.Columns.Add("FirstName", typeof(string));
            table.Columns.Add("LastName", typeof(string));
            table.Columns.Add("DisplayName", typeof(string));
            table.Columns.Add("IsSuperUser", typeof(byte));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("VanityUrl", typeof(string));
            table.Columns.Add("AffiliateId", typeof(int));
            table.Columns.Add("IsDeleted", typeof(byte));
            table.Columns.Add("RefreshRoles", typeof(byte));
            table.Columns.Add("LastIPAddress", typeof(string));
            table.Columns.Add("UpdatePassword", typeof(byte));
            table.Columns.Add("PasswordResetToken", typeof(Guid));
            table.Columns.Add("PasswordResetExpiration", typeof(DateTime));
            table.Columns.Add("Authorised", typeof(byte));

            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));

            table.Rows.Add(1, null, UserName1, UserName1, UserName1, UserName1, 1, "host@change.me", null, null, 0, null,
                           "127.0.0.1", 0, "8D3C800F-7A40-45D6-BA4D-E59A393F9800", DateTime.Now, null, -1, DateTime.Now,
                           -1, DateTime.Now);
            return table.CreateDataReader();
        }
        private IDataReader GetPortalGroups()
        {
            var table = new DataTable("ModuleDefinitions");
            var pkId = table.Columns.Add("PortalGroupID", typeof(int));
            table.Columns.Add("MasterPortalID", typeof(int));
            table.Columns.Add("PortalGroupName", typeof(string));
            table.Columns.Add("PortalGroupDescription", typeof(string));
            table.Columns.Add("AuthenticationDomain", typeof(string));
            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));
            table.PrimaryKey = new[] { pkId };

            table.Rows.Add(0, 0, "test", "descr", "domain", -1, DateTime.Now, -1, DateTime.Now);
            return table.CreateDataReader();
        }
        //return 2 test tabs(TabId 56 - Home,  TabId 57 - AboutUs)
        private IDataReader GetTabs()
        {
            var table = new DataTable("Tabs");
            table.Columns.Add("TabID", typeof(int));
            table.Columns.Add("TabOrder", typeof(int));
            table.Columns.Add("PortalID", typeof(int));
            table.Columns.Add("TabName", typeof(string));
            table.Columns.Add("ParentID", typeof(int));
            table.Columns.Add("Level", typeof(int));
            table.Columns.Add("TabPath", typeof(string));
            table.Columns.Add("UniqueId", typeof(Guid));
            table.Columns.Add("VersionGuid", typeof(Guid));
            table.Columns.Add("DefaultLanguageGuid", typeof(Guid));
            table.Columns.Add("LocalizedVersionGuid", typeof(Guid));
            table.Columns.Add("IsVisible", typeof(byte));
            table.Columns.Add("IconFile", typeof(string));
            table.Columns.Add("IconFileLarge", typeof(string));
            table.Columns.Add("DisableLink", typeof(byte));
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("KeyWords", typeof(string));
            table.Columns.Add("IsDeleted", typeof(byte));
            table.Columns.Add("SkinSrc", typeof(string));
            table.Columns.Add("ContainerSrc", typeof(string));
            table.Columns.Add("StartDate", typeof(DateTime));
            table.Columns.Add("EndDate", typeof(DateTime));
            table.Columns.Add("Url", typeof(string));
            table.Columns.Add("HasChildren", typeof(string));
            table.Columns.Add("RefreshInterval", typeof(int));
            table.Columns.Add("PageHeadText", typeof(string));
            table.Columns.Add("IsSecure", typeof(byte));
            table.Columns.Add("PermanentRedirect", typeof(byte));
            table.Columns.Add("SiteMapPriority", typeof(float));
            table.Columns.Add("ContentItemId", typeof(int));
            table.Columns.Add("Content", typeof(string));
            table.Columns.Add("ContentTypeID", typeof(int));
            table.Columns.Add("ModuleID", typeof(int));
            table.Columns.Add("ContentKey", typeof(string));
            table.Columns.Add("Indexed", typeof(byte));
            table.Columns.Add("StateID", typeof(int));
            table.Columns.Add("CultureCode", typeof(string));
            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));
            
            table.Rows.Add(56, 5, 0, "Home", null,	0,	"//Home", "C3174A2E-374D-4779-BE5F-BCDFF410E097", "A111A742-C18F-495D-8A23-BD0ECC70BBFE", null, "3A34424A-3CCA-4934-AE15-B9A80EB6D259",	1, null, null, 0, null, null, null, 0,	"[G]Skins/Gravity/Home.ascx", "[G]Containers/Gravity/Title_h2.ascx", null, null, null, "false", null, null, 0, 0,	0.5, 86,	"Home",	1,	-1,	null,	0,	null, null, -1,	DateTime.Now, -1, DateTime.Now);
            table.Rows.Add(57, 13, 0, "About Us", null, 0, "//AboutUs", "26A4236F-3AAA-4E15-8908-45D35675C677", "8426D3BC-E930-49CA-BDEB-4D41F194B6AC", null, "1461572D-97E8-41F8-BB1A-916DCA48890A", 1, null, null, 0, null, null, null, 0, "[G]Skins/Gravity/Home.ascx", "[G]Containers/Gravity/Title_h2.ascx", null, null, null, "true", null, null, 0, 0, 0.5, 97, "About Us", 1, -1, null, 0, null, null, -1, DateTime.Now, -1, DateTime.Now);
            

            return table.CreateDataReader();
        }
        //return 4 html modules (TabId 56 - Home: ModuleIDs:367, 368, 370  TabId 57 - AboutUs: 378//)
        private IDataReader GetSearchModules()
        {
            var table = new DataTable("SearchModules");
            table.Columns.Add("OwnerPortalID", typeof(int));
            table.Columns.Add("PortalID", typeof(int));
            table.Columns.Add("TabID", typeof(int));
            table.Columns.Add("TabModuleID", typeof(int));
            table.Columns.Add("ModuleID", typeof(int));
            table.Columns.Add("ModuleDefID", typeof(int));
            table.Columns.Add("ModuleOrder", typeof(int));
            table.Columns.Add("PaneName", typeof(string));
            table.Columns.Add("ModuleTitle", typeof(string));
            table.Columns.Add("CacheTime", typeof(int));
            table.Columns.Add("CacheMethod", typeof(string));
            table.Columns.Add("Alignment", typeof(string));
            table.Columns.Add("Color", typeof(string));
            table.Columns.Add("Border", typeof(string));
            table.Columns.Add("IconFile", typeof(string));
            table.Columns.Add("AllTabs", typeof(byte));
            table.Columns.Add("Visibility", typeof(int));
            table.Columns.Add("IsDeleted", typeof(byte));
            table.Columns.Add("Header", typeof(string));
            table.Columns.Add("Footer", typeof(string));
            table.Columns.Add("StartDate", typeof(DateTime));
            table.Columns.Add("EndDate", typeof(DateTime));
            table.Columns.Add("ContainerSrc", typeof(string));
            table.Columns.Add("DisplayTitle", typeof(byte));
            table.Columns.Add("DisplayPrint", typeof(byte));
            table.Columns.Add("DisplaySyndicate", typeof(byte));
            table.Columns.Add("IsWebSlice", typeof(byte));
            table.Columns.Add("WebSliceTitle", typeof(string));
            table.Columns.Add("WebSliceExpiryDate", typeof(DateTime));
            table.Columns.Add("WebSliceTTL", typeof(int));
            table.Columns.Add("InheritViewPermissions", typeof(int));
            table.Columns.Add("IsShareable", typeof(int));
            table.Columns.Add("IsShareableViewOnly", typeof(int));
            table.Columns.Add("DesktopModuleID", typeof(int));
            table.Columns.Add("DefaultCacheTime", typeof(int));
            table.Columns.Add("ModuleControlID", typeof(int));
            table.Columns.Add("BusinessControllerClass", typeof(string));
            table.Columns.Add("IsAdmin", typeof(byte));
            table.Columns.Add("SupportedFeatures", typeof(int));
            table.Columns.Add("ContentItemID", typeof(int));
            table.Columns.Add("Content", typeof(string));
            table.Columns.Add("ContentTypeID", typeof(int));
            table.Columns.Add("ContentKey", typeof(string));
            table.Columns.Add("Indexed", typeof(byte));
            table.Columns.Add("StateID", typeof(int));
            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));
            table.Columns.Add("LastContentModifiedOnDate", typeof(DateTime));
            table.Columns.Add("UniqueId", typeof(Guid));
            table.Columns.Add("VersionGuid", typeof(Guid));
            table.Columns.Add("defaultLanguageGuid", typeof(Guid));
            table.Columns.Add("localizedVersionGuid", typeof(Guid));
            table.Columns.Add("CultureCode", typeof(string));
            
            table.Rows.Add(0, 0, 56, 57, 368, 116, 1, "contentpane", "Text/HTML", 1200,
                           "FileModuleCachingProvider", null, null, null, "", 0, 0, 0, null, null, null, null,
                           "[G]Containers/Gravity/NoTitle.ascx", 1, 0, 0, 0, null, null, 0, 1, 1, 1,
                           74, 1200, 238,
                           "DotNetNuke.Modules.Html.HtmlTextController, DotNetNuke.Modules.Html", 0, 7, 92,
                           "Text/HTML", 2, null, 0, null, -1, "2014-02-18 10:39:45.170", -1,
                           "2014-02-18 10:39:45.170", "2014-02-18 10:39:45.190",
                           "A0B23459-676C-4DE4-BCA1-33E222F8405A", "85AF4947-EB80-475D-9D8D-0BAD6B026A2B", null,
                           "664BAA98-7E24-461F-8180-36527619D042", "");

            table.Rows.Add(0, 0, 56, 56, 367, 116, 1, "contentpane", "Header Images", 1200,
                           "FileModuleCachingProvider", null, null, null, "", 0, 0, 0, null, null, null, null,
                           "[G]Containers/Gravity/NoTitle.ascx", 1, 0, 0, 0, null, null, 0, 1, 1, 1,
                           74, 1200, 238,
                           "DotNetNuke.Modules.Html.HtmlTextController, DotNetNuke.Modules.Html", 0, 7, 91,
                           "Header Images", 2, null, 0, null, -1, "2014-02-18 10:39:45.170", -1,
                           "2014-02-18 10:39:45.170", "2014-02-18 10:39:45.190",
                           "A0B23459-676C-4DE4-BCA1-33E222F8405A", "85AF4947-EB80-475D-9D8D-0BAD6B026A2B", null,
                           "664BAA98-7E24-461F-8180-36527619D042", "");

            table.Rows.Add(0, 0, 56, 59, 370, 116, 1, "contentpane", "Customer Support", 1200,
                           "FileModuleCachingProvider", null, null, null, "", 0, 0, 0, null, null, null, null,
                           "[G]Containers/Gravity/NoTitle.ascx", 1, 0, 0, 0, null, null, 0, 1, 1, 1,
                           74, 1200, 238,
                           "DotNetNuke.Modules.Html.HtmlTextController, DotNetNuke.Modules.Html", 0, 7, 94,
                           "Customer Support", 2, null, 0, null, -1, "2014-02-18 10:39:45.170", -1,
                           "2014-02-18 10:39:45.170", "2014-02-18 10:39:45.190",
                           "A0B23459-676C-4DE4-BCA1-33E222F8405A", "85AF4947-EB80-475D-9D8D-0BAD6B026A2B", null,
                           "664BAA98-7E24-461F-8180-36527619D042", "");

            table.Rows.Add(0, 0, 57, 67, 378, 116, 1, "contentpane", "About Us", 1200,
                           "FileModuleCachingProvider", null, null, null, "", 0, 0, 0, null, null, null, null,
                           "[G]Containers/Gravity/NoTitle.ascx", 1, 0, 0, 0, null, null, 0, 1, 1, 1,
                           74, 1200, 238,
                           "DotNetNuke.Modules.Html.HtmlTextController, DotNetNuke.Modules.Html", 0, 7, 103,
                           "Text/HTML", 2, null, 0, null, -1, "2014-02-18 10:39:45.170", -1,
                           "2014-02-18 10:39:45.170", "2014-02-18 10:39:45.190",
                           "A0B23459-676C-4DE4-BCA1-33E222F8405A", "85AF4947-EB80-475D-9D8D-0BAD6B026A2B", null,
                           "664BAA98-7E24-461F-8180-36527619D042", "");
            return table.CreateDataReader();
        }
        //returns 2 moduledefinitions - Text/HTML and Journal
        private IDataReader GetModuleDefinitions()
        {
            var table = new DataTable("ModuleDefinitions");
            var pkId = table.Columns.Add("ModuleDefID", typeof(int));
            table.Columns.Add("FriendlyName", typeof(string));
            table.Columns.Add("DesktopModuleID", typeof(int));
            table.Columns.Add("DefaultCacheTime", typeof(int));
            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));
            table.Columns.Add("DefinitionName", typeof(string));
            table.PrimaryKey = new[] { pkId };

            table.Rows.Add(116, "Text/HTML", 74, 1200, -1, DateTime.Now, -1, DateTime.Now, "Text/HTML");
            table.Rows.Add(117, "Journal", 75, 0, -1, DateTime.Now, -1, DateTime.Now, "Journal");

            return table.CreateDataReader();
        }
        //returns all search types - 3 SearchTypes - module, tab, user
        private  IDataReader GetAllSearchTypes()
        {
            var table = new DataTable("SearchTypes");
            var pkId = table.Columns.Add("SearchTypeId", typeof(int));
            table.Columns.Add("SearchTypeName", typeof(string));
            table.Columns.Add("SearchResultClass", typeof(string));
            table.Columns.Add("IsPrivate", typeof(byte));
            table.PrimaryKey = new[] { pkId };

            table.Rows.Add(1, "module", FakeResultControllerClass, 0);
            table.Rows.Add(2, "tab", FakeResultControllerClass, 0);
            table.Rows.Add(3, "user", FakeResultControllerClass, 0);
            return table.CreateDataReader();
        }
        private IDataReader GetPortalsCallBack(string culture)
        {
            return GetPortalCallBack(PortalId0, CultureEnUs);
        }
        private IDataReader GetPortalCallBack(int portalId, string culture)
        {
            DataTable table = new DataTable("Portal");

            var cols = new string[]
                           {
                               "PortalID", "PortalGroupID", "PortalName", "LogoFile", "FooterText", "ExpiryDate",
                               "UserRegistration", "BannerAdvertising", "AdministratorId", "Currency", "HostFee",
                               "HostSpace", "PageQuota", "UserQuota", "AdministratorRoleId", "RegisteredRoleId",
                               "Description", "KeyWords", "BackgroundFile", "GUID", "PaymentProcessor",
                               "ProcessorUserId",
                               "ProcessorPassword", "SiteLogHistory", "Email", "DefaultLanguage", "TimezoneOffset",
                               "AdminTabId", "HomeDirectory", "SplashTabId", "HomeTabId", "LoginTabId", "RegisterTabId",
                               "UserTabId", "SearchTabId", "Custom404TabId", "Custom500TabId", "SuperTabId",
                               "CreatedByUserID", "CreatedOnDate", "LastModifiedByUserID", "LastModifiedOnDate",
                               "CultureCode"
                           };

            foreach (var col in cols)
            {
                table.Columns.Add(col);
            }

            var homePage = 1;
            table.Rows.Add(portalId, null, "My Website", "Logo.png", "Copyright 2011 by DotNetNuke Corporation", null,
                           "2", "0", "2", "USD", "0", "0", "0", "0", "0", "1", "My Website",
                           "DotNetNuke, DNN, Content, Management, CMS", null, "1057AC7A-3C08-4849-A3A6-3D2AB4662020",
                           null, null, null, "0", "admin@change.me", "en-US", "-8", "58", "Portals/0", null,
                           homePage.ToString(), null, null, "57", "56", "-1", "-1", "7", "-1", "2011-08-25 07:34:11",
                           "-1", "2011-08-25 07:34:29", culture);

            return table.CreateDataReader();
        }
        #endregion

        private IEnumerable<GroupedBasicView> GetGroupBasicViewResults(SearchQuery query)
       {
           var userSearchContentSource = new SearchContentSource
                                             {
                                                 SearchTypeId = UrlSearchTypeId,
                                                 SearchTypeName = UrlSearchTypeName,
                                                 SearchResultClass = FakeResultControllerClass,
                                                 LocalizedName = UserSearchTypeName,
                                                 ModuleDefinitionId = 0
                                             };
           var results = _searchServiceController.GetGroupedBasicViews(query, userSearchContentSource, PortalId0);
           return results;
       }
        private IEnumerable<GroupedDetailView> GetGroupedDetailViewResults(SearchQuery searchQuery)
        {
            bool more = false;
            int totalHits = 0;
            var results = _searchServiceController.GetGroupedDetailViews(searchQuery, UserSearchTypeId, out totalHits, out more);
            return results;
        }

        [Test]
        public void GetSearchResultsDetailed()
        {
            const string keyword = "super";
            const string moduleBody = "super content is here";
            const string userUrl = "mysite/userid/1";
            const string tabUrl1 = "mysite/Home";
            const string tabUrl2 = "mysite/AboutUs";

            //first tab with 2 modules
            var doc1 = new SearchDocument { UniqueKey = "key01", TabId = TabId1, Url = tabUrl1, Title = keyword, SearchTypeId = TabSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow };
            var doc2 = new SearchDocument { UniqueKey = "key02", TabId = TabId1, Title = keyword, Url = tabUrl1, SearchTypeId = ModuleSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, ModuleDefId = HtmlModuleDefId, ModuleId = HtmlModuleId2, Body = moduleBody, RoleId = 731};
            var doc3 = new SearchDocument { UniqueKey = "key03", TabId = TabId1, Title = keyword, Url = tabUrl1, SearchTypeId = ModuleSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, ModuleDefId = HtmlModuleDefId, ModuleId = HtmlModuleId1, Body = moduleBody, RoleId = 731 };
           
            //second tab with 1 module
            var doc4 = new SearchDocument { UniqueKey = "key04", TabId = TabId2, Url = tabUrl2, Title = keyword, SearchTypeId = TabSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, RoleId = RoleId0 };
            var doc5 = new SearchDocument { UniqueKey = "key05", TabId = TabId2, Title = keyword, Url = tabUrl2, SearchTypeId = ModuleSearchTypeId, ModuleDefId = HtmlModuleId, ModuleId = HtmlModuleId3, ModifiedTimeUtc = DateTime.UtcNow, Body = moduleBody, RoleId = 731 };
            
            //user doc
            var userdoc = new SearchDocument { UniqueKey = "key06", Url = userUrl, Title = keyword, SearchTypeId = UserSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, RoleId = RoleId731 };
            _internalSearchController.AddSearchDocument(doc1);
            _internalSearchController.AddSearchDocument(doc2);
            _internalSearchController.AddSearchDocument(doc3);
            _internalSearchController.AddSearchDocument(doc4);
            _internalSearchController.AddSearchDocument(doc5);
            _internalSearchController.AddSearchDocument(userdoc);

            var query = new SearchQuery
            {
                KeyWords = keyword,
                SearchTypeIds = new[] { ModuleSearchTypeId, TabSearchTypeId, UserSearchTypeId },
                RoleId = 731
            };

            //Run 
            var search = GetGroupedDetailViewResults(query);
            
            //Assert
            var groupedDetailViews = search as List<GroupedDetailView> ?? search.ToList();
            
            //Overall 3 groups - tab1, tab2 and user
            Assert.AreEqual(3, groupedDetailViews.Count());
            
            //Tab 1 has 2 DetailViews 
            Assert.AreEqual(2, groupedDetailViews.Single(x=>x.DocumentUrl==tabUrl1).Results.Count());

            //Tab 2 has 1 DetailViews 
            Assert.AreEqual(1, groupedDetailViews.Single(x => x.DocumentUrl == tabUrl2).Results.Count());

            //UserUrl has 1 DetailViews 
            Assert.AreEqual(1, groupedDetailViews.Single(x => x.DocumentUrl == userUrl).Results.Count());
        }
        [Test]
        public void GetSearchResultsBasic()
        {
            const string keyword = "awesome";
            const string userUrl = "mysite/userid/1";
            const string tabUrl1 = "mysite/Home";
            const string tabUrl2 = "mysite/AboutUs";

            var doc1 = new SearchDocument { UniqueKey = "key01", TabId = TabId1, Url = tabUrl1, Title = keyword, SearchTypeId = TabSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, RoleId = RoleId731 };
            var doc2 = new SearchDocument { UniqueKey = "key02", TabId = TabId2, Url = tabUrl2, Title = keyword, SearchTypeId = TabSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, RoleId = RoleId0 };
            var userdoc = new SearchDocument { UniqueKey = "key03", Url = userUrl, Title = keyword, SearchTypeId = UserSearchTypeId, ModifiedTimeUtc = DateTime.UtcNow, RoleId = RoleId0 };
            _internalSearchController.AddSearchDocument(doc1);
            _internalSearchController.AddSearchDocument(doc2);
            _internalSearchController.AddSearchDocument(userdoc);

            var query = new SearchQuery
            {
                KeyWords = keyword,
                PortalIds = new List<int> { PortalId0 },
                SearchTypeIds = new[] { ModuleSearchTypeId, TabSearchTypeId, UserSearchTypeId },
                ModuleDefIds = new List<int> { HtmlModDefId },
                BeginModifiedTimeUtc = DateTime.Now.AddHours(-1.00),
                EndModifiedTimeUtc = DateTime.Now > DateTime.MinValue ? DateTime.MaxValue : DateTime.MinValue,
                PageIndex = 1,
                PageSize = 15,
                SortField = 0,
                TitleSnippetLength = 120,
                BodySnippetLength = 300,
                CultureCode = CultureEnUs,
                WildCardSearch = true
            };

            //Run 
            var search = GetGroupBasicViewResults(query);
            //Assert
            //Overal 2 groups - tabs and users
            var groupedBasicViews = search as List<GroupedBasicView> ?? search.ToList();
            Assert.AreEqual(2, groupedBasicViews.Count());

            //1 User results 
            Assert.AreEqual(1, groupedBasicViews.Single(x=>x.DocumentTypeName=="user").Results.Count());

            //User result should have 1 attribute(avatar)
            Assert.AreEqual(1, groupedBasicViews.Single(x => x.DocumentTypeName == "user").Results.ElementAt(0).Attributes.Count());

            //2 Tabs results
            Assert.AreEqual(2, groupedBasicViews.Single(x => x.DocumentTypeName == "tab").Results.Count());
        }
    }
}