namespace wacs.Election
{
	public class ElectionResult
	{
		public Candidate Leader { get; set; }

		public CampaignStatus Status { get; set; }
	}

	public enum CampaignStatus
	{
		Timeout,
		Elected
	}
}