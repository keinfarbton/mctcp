/*
 * main.cpp
 *
 *  Created on: 16.07.2016
 *      Author: Frank
 */


#include <cstdlib>
#include <iostream>
#include <iomanip>
#include <memory>
#include <utility>
#include <boost/asio.hpp>
#include <boost/format.hpp>

#include "network/ServerCtrl.h"
#include "network/ServerChabu.h"

int main3(int argc, char* argv[]) {
	try {
		if (argc != 2) {
			std::cerr << "Usage: async_tcp_echo_server <port>\n";
			return 1;
		}

		boost::asio::io_service io_service;

		int port = std::atoi(argv[1]);

		network::ServerCtrl  serverCtrl (io_service, port+0 );
		network::ServerChabu serverChabu(io_service, port+1 );

		std::cout << "server at " << port << std::endl;
		io_service.run();
	}
	catch (std::exception& e) {
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}

#include <pugixml.hpp>
#include <fstream>
#include "testprot/XferItem.h"


using std::string;
using std::shared_ptr;
using std::vector;


using namespace testprot;

int main(int argc, char **argv) {
	std::ifstream stream("example.xml");
	pugi::xml_document doc;
	pugi::xml_parse_result result = doc.load(stream);
	try{
		if( result ){
			auto root = doc.root();
			XferItem xferItem;
			xferItem.load(root.child("XferItem"));
			std::cout << "XferItem.toString: "  << xferItem.toString();
		}
		else {
			std::cout << "error: " <<
			result.description() << std::endl;
		}
	}
	catch( std::exception& e ){
		std::cout << "exception: " << e.what();
	}
}

