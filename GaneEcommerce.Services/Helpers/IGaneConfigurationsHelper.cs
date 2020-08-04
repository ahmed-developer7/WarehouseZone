using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Ganedata.Core.Services
{
    public interface IGaneConfigurationsHelper
    {
        Boolean ApiErrorNotification(string body, int tenantId);

        string GetRecipientEmailForAccount(int? accountId, int accountContactId = 0);
        List<MailAddress> GetRecipientEmailsForOrder(Order order);
        Task<string> DispatchMailNotification(TenantEmailNotificationQueue notification, int tenantId, WorksOrderNotificationTypeEnum worksOrderNotificationType = WorksOrderNotificationTypeEnum.WorksOrderLogTemplate, bool sendImmediately = true,int? accountId=null);

        Task<string> SendStandardMailNotification(int tenantId, string subject, string bodyHtml, string footerHtml, string recipients, bool salesRequiresAuthorisation = true);
        Task UpdateIsCurrentTenantFlags();
        string TranslateEmailTemplateForOrder(TenantEmailNotificationQueue notificationItem, int tenantId, WorksOrderNotificationTypeEnum worksOrderNotificationType = WorksOrderNotificationTypeEnum.WorksOrderLogTemplate);

        Task<string> CreateTenantEmailNotificationQueue(string subject,
            OrderViewModel order, string attachmentVirtualPath = null, bool sendImmediately = true,
            DateTime? scheduleStartTime = null, DateTime? scheduleEndTime = null, string scheduleResourceName = null,
            OrderRecipientInfo shipmentAndRecipientInfo = null,
            WorksOrderNotificationTypeEnum worksOrderNotificationType =
                WorksOrderNotificationTypeEnum.WorksOrderLogTemplate, Appointments appointment = null);

        Task<string> DispatchTenantEmailNotificationQueues(int tenantId);
        string GetMailMergeVariableValueFromOrder(Order order, MailMergeVariableEnum variableType, TenantEmailNotificationQueue notificationItem = null);
        bool IsPOComplete(int? POID, int UserId, int warehouseId);
        bool ActiveStocktake(int warehouseId);
        Boolean GetStStatus(int id);
        string GetStStatusString(int StatusCode);
        string GetDeviceLastIp(string serial);
        string GetDeviceLastPingDate(string serial);
        bool GetDeviceCurrentStatus(string serial);
        bool SendMail(string subject, string body, int tenantId);
        string GetPropertyFirstline(int pPropertyId);
        string GetActionResultHtml(Controller controller, string partialViewName, object model);
        Task<string> SendStandardMailProductGroup(int tenantId, string subject, int accountId);
    }
}