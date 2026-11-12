using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace StockPilot.Entity
{
	[Table("DocumentSequences")]
	public class DocumentSequence : Abp.Domain.Entities.Entity, IMustHaveTenant, IHasModificationTime
	{
		public virtual int TenantId { get; set; }

		[Required]
		[StringLength(10)]
		public virtual string DocumentType { get; set; }

		public virtual int NextNumber { get; set; } = 1;

		public virtual DateTime? LastModificationTime { get; set; }

		[Timestamp]
		public virtual byte[] RowVersion { get; set; }
	}
}
