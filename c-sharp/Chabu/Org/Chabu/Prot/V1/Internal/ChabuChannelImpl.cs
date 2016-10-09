/*******************************************************************************
 * The MIT License (MIT)
 * Copyright (c) 2015 Frank Benoit, Stuttgart, Germany <fr@nk-benoit.de>
 * 
 * See the LICENSE.md or the online documentation:
 * https://docs.google.com/document/d/1Wqa8rDi0QYcqcf0oecD8GW53nMVXj3ZFSmcF81zAa8g/edit#heading=h.2kvlhpr5zi2u
 * 
 * Contributors:
 *     Frank Benoit - initial API and implementation
 *******************************************************************************/
using System;
using System.Diagnostics;
using Org.Chabu.Prot.V1;

namespace Org.Chabu.Prot.V1.Internal
{
    using global::System;
    using ByteBuffer = global::System.IO.MemoryStream;
    using PrintWriter = global::System.IO.TextWriter;

    /**
     *
     * @author Frank Benoit
     */
    internal sealed class ChabuChannelImpl : ChabuChannel {

	    private int     channelId    = -1;
	
	    private ChabuImpl                   chabu;
	    private readonly ChabuRecvByteTarget recvTarget;
	    private readonly ChabuXmitByteSource xmitSource;
	
	    private int        xmitSeq = 0;
	    private int        xmitArm = 0;
	
	    private bool    recvArmShouldBeXmit  = false;
	
	    private int        recvSeq = 0;
	    private int        recvArm = 0;
	
	    private int        priority = 0;

	    private ByteBuffer recvTargetBuffer;

	    private long xmitLimit;
	    private long xmitPosition;
	    private long recvLimit;
	    private long recvPosition;

	
	    public ChabuChannelImpl(int priority, ChabuRecvByteTarget recvTarget, ChabuXmitByteSource xmitSource ) {
		    this.recvTarget = recvTarget;
		    this.xmitSource = xmitSource;
		    Utils.ensure( priority >= 0, ChabuErrorCode.CONFIGURATION_CH_PRIO, "priority must be >= 0, but is {0}", priority );
		    Utils.ensure( recvTarget != null, ChabuErrorCode.CONFIGURATION_CH_USER, "IChabuChannelUser must be non null" );
		    Utils.ensure( xmitSource != null, ChabuErrorCode.CONFIGURATION_CH_USER, "IChabuChannelUser must be non null" );

		    this.recvArmShouldBeXmit = true;

		    this.priority = priority;
	    }
	
	    public void activate(ChabuImpl chabu, int channelId ){

		    this.chabu      = chabu;
		    this.channelId  = channelId;

		    chabu.channelXmitRequestArm(channelId);
		
		    recvTarget.setChannel(this);
		    if( recvTarget != xmitSource ){
			    xmitSource.setChannel(this);
		    }

	    }
	
	    internal void verifySeq(int packetSeq ) {
		    Utils.ensure( this.recvSeq == packetSeq, ChabuErrorCode.PROTOCOL_DATA_OVERFLOW, 
				    "Channel[{0}] received more seq but expected ({1} :: {2}). Violation of the SEQ value.\n >> {3}", 
				    channelId, packetSeq, this.recvSeq, this );
	    }
	
	    internal int handleRecvSeq(ByteChannel byteChannel, int recvByteCount ) {
		
		    int allowedRecv = this.recvArm - this.recvSeq;
		    int remainingBytes = recvByteCount;
		    Utils.ensure( remainingBytes <= allowedRecv, ChabuErrorCode.PROTOCOL_DATA_OVERFLOW, 
				    "Channel[{0}] received more data ({1}) as it can take ({2}). Violation of the ARM value.", channelId, remainingBytes, allowedRecv );
			
		    int summedReadBytes = 0;
		    while( remainingBytes > 0 ){
	
			    if( recvTargetBuffer == null ){
				
				    recvTargetBuffer = recvTarget.getRecvBuffer(remainingBytes);
				
				    Utils.ensure( recvTargetBuffer != null, ChabuErrorCode.ASSERT, 
						    "Channel[{0}] recvTargetBuffer is null.", channelId );
				
				    Utils.ensure( recvTargetBuffer.remaining() <= remainingBytes, ChabuErrorCode.ASSERT, 
						    "Channel[{0}] recvTargetBuffer has more remaining ({1}) as requested ({2}).", 
						    channelId, recvTargetBuffer.remaining(), remainingBytes );
				
				    Utils.ensure( recvTargetBuffer.remaining() > 0, ChabuErrorCode.ASSERT, 
						    "Channel[{0}] recvTargetBuffer cannot take data.", 
						    channelId );
			    }
			
			    int readBytes = byteChannel.read(recvTargetBuffer);
			    summedReadBytes += readBytes;
			    remainingBytes -= readBytes;
			
			    recvSeq += readBytes;
			    recvPosition += readBytes;
			
			    if( !recvTargetBuffer.hasRemaining() ){
				    recvTargetBuffer = null;
				    recvTarget.recvCompleted();
			    }
			
			    if( readBytes == 0 ){
				    // could not read => try next time
				    break;
			    }
		    }	
		    return summedReadBytes;
	    }
	
	    /**
	     * Receive the ARM from the partner. This may make the channel to prepare new data to send.
	     * @param arm the value to update this.xmitArm to.
	     */
	    internal void handleRecvArm(int arm) {
		    if( this.xmitArm != arm ){
			    // was blocked by receiver
			    // now the arm is updated
			    // --> try to send new data
			    if( getXmitRemaining() > 0 ){
				    chabu.channelXmitRequestData(channelId);
			    }
		    }
		    this.xmitArm = arm;
	    }

	    public void handleXmitCtrl(ChabuXmitterNormal xmitter, ByteBuffer xmitBuf) {
		    if( recvArmShouldBeXmit ) {
			    recvArmShouldBeXmit = false;
			    xmitter.processXmitArm(channelId, recvArm );
		    }
	    }

	    public ByteBuffer handleXmitData(ChabuXmitterNormal xmitter, ByteBuffer xmitBuf, int maxSize) {
		    int davail = Math.Min( getXmitRemainingByRemote(), getXmitRemaining() );
		    if( davail == 0 ){
			    //System.out.println("ChabuChannelImpl.handleXmitData() : called by no data available");
			    return null;
		    }
		    int pls = Math.Min( davail, maxSize );
	
		    ByteBuffer seqBuffer = xmitSource.getXmitBuffer(pls);
		
		    int realPls = seqBuffer.remaining();
		    Utils.ensure( realPls > 0, ChabuErrorCode.ASSERT, "XmitSource gave buffer with no space" );
		    Utils.ensure( realPls <= pls, ChabuErrorCode.ASSERT, "XmitSource gave buffer with more data than was requested" );
		
		    xmitter.processXmitSeq(channelId, xmitSeq, realPls );
		    xmitSeq += realPls;
		    xmitPosition += realPls;
		
		    return seqBuffer;
	    }

	    public String toString(){
		    return String.Format("Channel[{0} recvS:{1} recvA:{2} recvPostion:{3} recvLimit:{4} xmitS:{5} xmitA:{6} xmitPostion:{7} xmitLimit:{8}]", channelId, this.recvSeq, this.recvArm, this.recvPosition, this.recvLimit, this.xmitSeq, this.xmitArm, this.xmitPosition, this.xmitLimit );
	    }

	    public int getChannelId() {
		    return channelId;
	    }

	    public int getPriority() {
		    return priority;
	    }

	    public void setXmitLimit(long xmitLimit) {
		    int added = Utils.safePosInt( xmitLimit - this.xmitLimit );
		    addXmitLimit(added);
	    }

	    public long getXmitLimit() {
		    return xmitLimit;
	    }

	    public long addXmitLimit(int added) {
		    if( added > 0 ){
			    this.xmitLimit += added;
			    chabu.channelXmitRequestData(channelId);
		    }
		    return xmitLimit;
	    }

	    public int getXmitRemaining() {
		    return Utils.safePosInt( xmitLimit - xmitPosition );
	    }
	
	    public int getXmitRemainingByRemote() {
		    return xmitArm - xmitSeq;
	    }


	    public long getXmitPosition() {
		    return xmitPosition;
	    }

	    /**
	     * Called from Chabu, when the SEQ packet was transmitted.
	     */
	    public void seqPacketCompleted() {
		    xmitSource.xmitCompleted();
	    }

	    public void setRecvLimit(long recvLimit) {
		    int added = Utils.safePosInt( recvLimit - this.recvLimit );
		    addRecvLimit(added);
	    }

	    public long addRecvLimit(int added) {
		    recvLimit += added;
		    recvArm += added;
		    recvArmShouldBeXmit = true;
		    chabu.channelXmitRequestArm(channelId);
		    return recvLimit;
	    }

	    public long getRecvLimit() {
		    return recvLimit;
	    }

	    public long getRecvPosition() {
		    return recvPosition;
	    }

	    public long getRecvRemaining() {
		    return Utils.safePosInt(recvLimit - recvPosition);
	    }
    }
}