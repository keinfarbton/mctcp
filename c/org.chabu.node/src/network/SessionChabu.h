/*
 * Session.h
 *
 *  Created on: 18.07.2016
 *      Author: fbenoit1
 */

#ifndef SESSION_H_
#define SESSION_H_

#include <boost/asio.hpp>

using boost::asio::ip::tcp;
namespace network {

class SessionChabu : public std::enable_shared_from_this<SessionChabu>
{
	tcp::socket socket_;
	enum { max_length = 1024 };
	char data_[max_length];

public:
	SessionChabu(tcp::socket socket);
	virtual ~SessionChabu();

	void start();

private:
	void do_read();

	void do_write(std::size_t length);

};
}
#endif /* SESSION_H_ */
