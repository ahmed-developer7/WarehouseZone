using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ganedata.Core.Entities.Domain
{
    [Serializable]
    public class ProductAttributes
    {
        public ProductAttributes()
        {
            ProductAttributeValues = new HashSet<ProductAttributeValues>();
        }
        [Key]
        [Display(Name = "Attribute Id")]
        public int AttributeId { get; set; }
        [Required]
        [StringLength(50)]
        [Display(Name = "Attribute Name")]
        public string AttributeName { get; set; }
        public virtual ICollection<ProductAttributeValues> ProductAttributeValues { get; set; }
    }
}
