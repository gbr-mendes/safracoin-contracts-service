// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract TokenAuthorizationOracle {
    mapping(address => bool) private authorizedAddresses;

    event AddressAuthorizationChanged(address indexed user, bool isAuthorized);

    modifier onlyAuthorized() {
        require(authorizedAddresses[msg.sender], "Address not authorized");
        _;
    }

    function setAuthorization(address _address, bool _isAuthorized) public {
        authorizedAddresses[_address] = _isAuthorized;
        emit AddressAuthorizationChanged(_address, _isAuthorized);
    }

    function isAuthorized(address _address) public view returns (bool) {
        return authorizedAddresses[_address];
    }
}
