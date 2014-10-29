package chabu.tester.data;

import java.nio.ByteBuffer;

import chabu.Utils;


public abstract class ACommand extends AXferItem {
	
	protected ACommand(CommandId commandId) {
		this.commandId = commandId;
	}
	
	public final CommandId commandId;
			
	public static ACommand decodeCommand(ByteBuffer buf) {
		//System.out.printf("ACommand.decode %s\n", buf);
		if( buf.remaining() < 4 ) return null;
		
		int pos = buf.position();
		int packetSz = buf.getInt();
		
		//System.out.printf("ACommand.decode pos:%s sz:%s\n", pos, packetSz);
		if( buf.remaining() < packetSz ){
			buf.position(pos);
			return null;
		}
		
		ACommand res = null;
		try{
			int cmd = buf.get();
			if( cmd == CommandId.TIME_BROADCAST.getId() ){
				res = CmdTimeBroadcast.createTimeBroadcast(buf);
			}
			else if( cmd == CommandId.DUT_APPLICATION_CLOSE.getId() ){
				res = CmdDutApplicationClose.createDutApplicationClose(buf);
			}
			else if( cmd == CommandId.CONNECTION_CLOSE.getId() ){
				res = CmdDutApplicationClose.createDutApplicationClose(buf);
			}
			else if( cmd == CommandId.CONNECTION_CONNECT.getId() ){
				res = CmdConnectionConnect.createConnectionStart(buf);
			}
			else if( cmd == CommandId.CONNECTION_AWAIT.getId() ){
				res = CmdConnectionAwait.createConnectionAwait(buf);
			}
			else if( cmd == CommandId.CHANNEL_ADD.getId() ){
				res = CmdChannelAdd.createChannelAdd(buf);
			}
			else if( cmd == CommandId.CHANNEL_ACTION.getId() ){
				res = CmdChannelAction.createChannelAction(buf);
			}
			else if( cmd == CommandId.CHANNEL_CREATE_STAT.getId() ){
				res = CmdChannelCreateStat.createChannelCreateStat(buf);
			}
			else {
				throw Utils.fail( "unknown command id %d", cmd );
			}
		}
		finally{
			if( res != null ){
				Utils.ensure( pos + packetSz + 4 == buf.position(), "%d %d +4 == %s", pos, packetSz, buf );
			}
			else {
				buf.position(pos);
			}
		}
		return res;
	}

	
}
