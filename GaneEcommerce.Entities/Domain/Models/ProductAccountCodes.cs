using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ganedata.Core.Entities.Domain
{
    [Serializable]
    public class ProductAccountCodes : PersistableEntity<int>
    {

        [Key]
        [Display(Name = "Product Code Id")]
        public int ProdAccCodeID { get; set; }
        [Required]
        [Display(Name = "Account")]
        public int AccountID { get; set; }
        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }
        [Remote("IsCodeAvailable","ProductAccounts",AdditionalFields = "AccountID,ProdAccCodeID", ErrorMessage ="Product Code for this account already exists")]
        [Required]
        [Display(Name = "Product Code")]
        public string ProdAccCode { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        public virtual ProductMaster ProductMaster { get; set; }
        public virtual Account Account { get; set; }
    }
}