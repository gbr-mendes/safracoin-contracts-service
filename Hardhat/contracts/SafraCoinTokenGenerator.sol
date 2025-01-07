// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

interface TokenAuthorizationOracle {
    function isAuthorized(address account) external view returns (bool);
}

contract SafraCoinTokenGenerator is ERC20, Ownable {
    address public oracleContract;

    event TokensMinted(address indexed to, uint256 amount);

    constructor(string memory name, string memory symbol, address _oracleContract)
        ERC20(name, symbol)
        Ownable(msg.sender)
    {
        oracleContract = _oracleContract;
    }

    function setOracle(address _oracleContract) external onlyOwner {
        oracleContract = _oracleContract;
    }

    function mint(address to, uint256 amount, address platform, uint256 platformFee) external {
        TokenAuthorizationOracle oracle = TokenAuthorizationOracle(oracleContract);
        require(oracle.isAuthorized(to), "You are not authorized to mint tokens");

        uint256 platformTokens = amount * platformFee;

        _mint(to, amount);

        _transfer(to, platform, platformTokens);

        emit TokensMinted(to, amount);
    }
}
