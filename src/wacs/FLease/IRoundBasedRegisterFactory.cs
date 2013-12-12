namespace wacs.FLease
{
	public interface IRoundBasedRegisterFactory
	{
		IRoundBasedRegister Build(INode owner);
	}
}