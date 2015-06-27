/*******************************************************************************
 * The MIT License (MIT)
 * Copyright (c) 2015 Frank Benoit.
 * 
 * See the LICENSE.md or the online documentation:
 * https://docs.google.com/document/d/1Wqa8rDi0QYcqcf0oecD8GW53nMVXj3ZFSmcF81zAa8g/edit#heading=h.2kvlhpr5zi2u
 * 
 * Contributors:
 *     Frank Benoit - initial API and implementation
 *******************************************************************************/
package org.chabu.internal;

import java.io.PrintWriter;
import java.nio.ByteBuffer;

import org.chabu.ChabuErrorCode;
import org.chabu.IChabuChannel;
import org.chabu.IChabuChannelUser;



public final class ChabuChannel implements IChabuChannel {

	private int     channelId    = -1;
	
	private Chabu        chabu;
	private final IChabuChannelUser user;
	
	private int        xmitSeq = 0;
	private int        xmitArm = 0;
	
	private boolean    recvArmShouldBeXmit  = false;
	
	private final ByteBuffer recvBuffer;
	private int        recvSeq = 0;
	private int        recvArm = 0;
	
	private int        priority = 0;
	
	public ChabuChannel(int recvBufferSize, int priority, IChabuChannelUser user ) {
		
		Utils.ensure( recvBufferSize > 0, ChabuErrorCode.CONFIGURATION_CH_RECVSZ, "recvBufferSize must be > 0, but is %s", recvBufferSize );
		Utils.ensure( priority >= 0, ChabuErrorCode.CONFIGURATION_CH_PRIO, "priority must be >= 0, but is %s", priority );
		Utils.ensure( user != null, ChabuErrorCode.CONFIGURATION_CH_USER, "IChabuChannelUser must be non null" );

		this.recvBuffer  = ByteBuffer.allocate(recvBufferSize);
		this.recvArm = recvBufferSize;
		this.recvArmShouldBeXmit = true;

		this.priority = priority;

		this.user = user;
		
	}
	
	void activate(Chabu chabu, int channelId ){

		this.chabu      = chabu;
		this.channelId  = channelId;

		user.setChannel(this);
		chabu.channelXmitRequestArm(channelId);
		
	}
	
	public void evUserXmitRequest(){
		chabu.channelXmitRequestData(channelId);
	}

	@Override
	public void evUserRecvRequest() {
		callUserToTakeRecvData();
	}

	private void callUserToTakeRecvData() {
		synchronized(this){
			recvBuffer.flip();
			int avail = recvBuffer.remaining();
			
			// prepare trace
			PrintWriter trc = chabu.getTraceWriter();
			int trcStartPos = recvBuffer.position();
			
			user.evRecv( recvBuffer );
			
			int consumed = avail - recvBuffer.remaining();
			
			// write out trace info
			if( trc != null && consumed > 0 ){
				trc.printf( "CHANNEL_TO_APPL: { \"ID\" : %s }%n", channelId );
				Utils.printTraceHexData(trc, recvBuffer, trcStartPos, recvBuffer.position());
			}
			
			recvBuffer.compact();
			if( consumed > 0 ){
				this.recvArm += consumed;
				this.recvArmShouldBeXmit = true;
				chabu.channelXmitRequestArm(channelId);
			}
		}
	}

	void handleRecvSeq(int seq, ByteBuffer buf, int pls ) {
		
		int allowedRecv = this.recvArm - this.recvSeq;
		
		
		Utils.ensure( pls <= allowedRecv, ChabuErrorCode.PROTOCOL_DATA_OVERFLOW, "Channel[%s] received more data (%s) as it can take (%s). Violation of the ARM value.", channelId, buf.remaining(), allowedRecv );
		Utils.ensure( buf.remaining() <= recvBuffer.remaining(), ChabuErrorCode.PROTOCOL_CHANNEL_RECV_OVERFLOW, "Channel[%s] received more data (%s) as it can take (%s). Violation of the ARM value.", channelId, buf.remaining(), recvBuffer.remaining() );
		
		int taken = Utils.transferUpTo( buf, recvBuffer, pls );
		
		Utils.ensure( taken == pls, ChabuErrorCode.PROTOCOL_CHANNEL_RECV_OVERFLOW, "Channel[%s] received more data (%s) as it can take (%s). Violation of the ARM value.", channelId, buf.remaining(), recvBuffer.remaining() );
		//recvBuffer.put( buf );
		this.recvSeq += pls;
		
		int align = pls;
		while( (align&3) != 0 ){
			align++;
			buf.get();
		}
		
		callUserToTakeRecvData();
	}
	
	/**
	 * Receive the ARM from the partner.
	 * @param arm
	 */
	void handleRecvArm(int arm) {
		if( this.xmitArm != arm && this.xmitArm == this.xmitSeq ){
			// was blocked by receiver
			// now the arm is updated
			// --> try to send new data
			chabu.channelXmitRequestData(channelId);
		}
		this.xmitArm = arm;
	}

	void handleXmitArm() {
		
		if( !recvArmShouldBeXmit ) {
			System.err.println("handleXmitArm()");
		}
		
		recvArmShouldBeXmit = false;
		chabu.processXmitArm(channelId, recvArm);
		chabu.channelXmitRequestData(channelId);

	}
	void handleXmitData() {
		chabu.processXmitSeq( channelId, xmitSeq, this::callUserToGiveXmit );
	}

	private void callUserToGiveXmit(ByteBuffer buf) {
		PrintWriter trc = chabu.getTraceWriter();
		int startPos = buf.position();
		
		user.evXmit(buf);
		
		int added = buf.position() - startPos;
		this.xmitSeq += added;
		
		// write out trace info
		if( trc != null && buf.position() != startPos ){
			trc.printf( "APPL_TO_CHANNEL: { \"ID\" : %s }%n", channelId );
			Utils.printTraceHexData(trc, buf, startPos, buf.position());
		}


	}
	
	public String toString(){
		return String.format("Channel[%s recvS:%s recvA:%s xmitS:%s xmitA:%s]", channelId, this.recvSeq, this.recvArm, this.xmitSeq, this.xmitArm );
	}

	@Override
	public int getChannelId() {
		return channelId;
	}

	@Override
	public int getPriority() {
		return priority;
	}

	@Override
	public IChabuChannelUser getUser() {
		return user;
	}

}
