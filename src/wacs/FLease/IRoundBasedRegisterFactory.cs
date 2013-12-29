namespace wacs.FLease
{
	public interface IRoundBasedRegisterFactory
	{
		IRoundBasedRegister Build(IProcess owner);
	}
}