// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";

interface SafraCoinTokenGenerator {
    function mint(address to, uint256 amount, address platform, uint256 platformFee) external;
}

contract SafraCoinPoolManagement {
    struct Crop {
        address farmer;
        uint256 totalTokens;
        uint256 platformFee;
        bool tokenized;
    }

    address public tokenGeneratorContract;
    address public platform;

    mapping(string => Crop) public crops;

    event CropCreated(string indexed cropId, address indexed farmer, uint256 totalTokens);

    constructor(address _platform, address _tokenGeneratorContract) {
        tokenGeneratorContract = _tokenGeneratorContract;
        platform = _platform;
    }

    function createCrop(
        string memory _cropId, 
        address _farmer, 
        uint256 _totalTokens, 
        uint256 _platformFee
    ) external {
        require(_platformFee <= 10, "Platform fee cannot exceed 10%");

        crops[_cropId] = Crop({
            farmer: _farmer,
            totalTokens: _totalTokens,
            platformFee: _platformFee,
            tokenized: false
        });

        emit CropCreated(_cropId, _farmer, _totalTokens);

        tokenizeCrop(_cropId);
    }

    function tokenizeCrop(string memory cropId) internal {
        Crop storage crop = crops[cropId];
        require(!crop.tokenized, "Crop already tokenized");

        crop.tokenized = true;

        SafraCoinTokenGenerator tokenGenerator = SafraCoinTokenGenerator(tokenGeneratorContract);

        // Mint tokens
        tokenGenerator.mint(crop.farmer, crop.totalTokens, platform, crop.platformFee);
    }
}
