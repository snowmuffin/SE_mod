using ProtoBuf;
using System;
using System.ComponentModel;

namespace SEUpgrademodule
{
    [ProtoContract]
    public class UpgradeModuleSummary
    {

		public static readonly Guid StorageGuid = new Guid("E51D3AD9-DD3C-4829-AE41-326B97773AE4");

		[ProtoMember(1), DefaultValue(0)]
        public int DefenseUpgradeLevel =0;
        [ProtoMember(2), DefaultValue(0)]
        public int AttackUpgradeLevel =0;
        [ProtoMember(3), DefaultValue(0)]
        public int PowerEfficiencyUpgradeLevel= 0;

        [ProtoMember(4), DefaultValue(0)]
        public int DampenerUpgradeLevel= 0;
		public UpgradeModuleSummary (int value1,int value2, int value3, int value4)
		{
			this.DefenseUpgradeLevel = value1;
            this.AttackUpgradeLevel = value2;
            this.PowerEfficiencyUpgradeLevel = value3;
            this.DampenerUpgradeLevel = value4;
		}
		
		public UpgradeModuleSummary ()
		{
		}

    }
}