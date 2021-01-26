﻿namespace STOContractTests
{
    using Moq;
    using Stratis.SmartContracts;
    using Xunit;
    using SalePeriod = STOContract.SalePeriod;
    using SalePeriodInput = STOContract.SalePeriodInput;
    using TokenType = STOContract.TokenType;

    public class STOContractTests
    {
        private const ulong Satoshis = 100_000_000;

        private readonly Mock<ISmartContractState> mContractState;
        private readonly Mock<IContractLogger> mContractLogger;
        private readonly Mock<IInternalTransactionExecutor> mTransactionExecutor;
        private readonly Mock<ISerializer> mSerializer;
        private readonly Mock<IBlock> mBlock;

        private Address sender;
        private Address owner;
        private Address investor;
        private Address identity;
        private Address contract;
        private Address tokenContract;
        private Address kycContract;
        private Address mapperContract;

        private InMemoryState persistentState;
        private UInt256 totalSupply;
        private string name;
        private string symbol;
        private uint decimals;

        public STOContractTests()
        {
            this.mContractLogger = new Mock<IContractLogger>();
            this.mContractState = new Mock<ISmartContractState>();
            this.mTransactionExecutor = new Mock<IInternalTransactionExecutor>();
            this.persistentState = new InMemoryState();
            this.mBlock = new Mock<IBlock>();
            this.mContractState.Setup(s => s.Block).Returns(this.mBlock.Object);
            this.mContractState.Setup(s => s.PersistentState).Returns(this.persistentState);
            this.mContractState.Setup(s => s.ContractLogger).Returns(this.mContractLogger.Object);
            this.mContractState.Setup(s => s.InternalTransactionExecutor).Returns(this.mTransactionExecutor.Object);
            this.mSerializer = new Mock<ISerializer>();
            this.mContractState.Setup(s => s.Serializer).Returns(this.mSerializer.Object);
            this.sender = "0x0000000000000000000000000000000000000001".HexToAddress();
            this.owner = "0x0000000000000000000000000000000000000002".HexToAddress();
            this.investor = "0x0000000000000000000000000000000000000003".HexToAddress();
            this.identity = "0x0000000000000000000000000000000000000004".HexToAddress();
            this.contract = "0x0000000000000000000000000000000000000005".HexToAddress();
            this.tokenContract = "0x0000000000000000000000000000000000000006".HexToAddress();
            this.kycContract = "0x0000000000000000000000000000000000000007".HexToAddress();
            this.mapperContract = "0x0000000000000000000000000000000000000008".HexToAddress();

            this.name = "Test Token";
            this.symbol = "TST";
            this.totalSupply = 100 * Satoshis;
            this.decimals = 0;
            this.persistentState.IsContractResult = true;
        }

        private ICreateResult createSucceed()
        {
            var mock = new Mock<ICreateResult>();

            mock.SetupGet(m => m.Success).Returns(true);
            mock.SetupGet(m => m.NewContractAddress).Returns(this.tokenContract);

            return mock.Object;
        }

        private ICreateResult createFailed()
        {
            var mock = new Mock<ICreateResult>();

            mock.SetupGet(m => m.Success).Returns(false);

            return mock.Object;
        }

        private ITransferResult transferSucceed(object returnValue = null)
        {
            var mock = new Mock<ITransferResult>();

            mock.SetupGet(m => m.Success).Returns(true);
            mock.SetupGet(m => m.ReturnValue).Returns(returnValue);

            return mock.Object;
        }

        private ITransferResult transferFailed()
        {
            var mock = new Mock<ITransferResult>();

            mock.SetupGet(m => m.Success).Returns(false);

            return mock.Object;
        }

        [Fact]
        public void Constructor_IsContract_ReturnsFalse_ThrowsAssertException()
        {
            this.persistentState.IsContractResult = false;

            Assert.Throws<SmartContractAssertException>(() => this.Create(TokenType.StandardToken));
        }

        [Fact]
        public void Constructor_TokenType_HigherThan2_ThrowsAssertException()
        {
            var tokenType = (TokenType)3;

            Assert.Throws<SmartContractAssertException>(() => this.Create(tokenType));
        }

        [Fact]
        public void Constructor_CreateReturnsFailedResult_ThrowsAssertException()
        {
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createFailed());
            Assert.Throws<SmartContractAssertException>(() => this.Create(TokenType.StandardToken));

            this.mTransactionExecutor.Verify(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>()), Times.Once);
        }

        [Fact]
        public void Constructor_TokenTypeIsStandardToken_Success()
        {
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            var (contract, periods) = this.Create(TokenType.StandardToken);

            Assert.Equal(this.totalSupply, contract.TokenBalance);
            Assert.Equal(this.owner, contract.Owner);
            Assert.Equal(this.tokenContract, contract.TokenAddress);
            Assert.Equal(4ul, contract.EndBlock);
            Assert.Equal(periods, contract.SalePeriods);
            this.mTransactionExecutor.Verify(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, 0), Times.Once);
        }

        [Fact]
        public void Constructor_TokenTypeIsDividendToken_Success()
        {
            this.mTransactionExecutor.Setup(m => m.Create<DividendToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            var (contract, periods) = this.Create(TokenType.DividendToken);

            Assert.Equal(this.totalSupply, contract.TokenBalance);
            Assert.Equal(this.owner, contract.Owner);
            Assert.Equal(this.tokenContract, contract.TokenAddress);
            Assert.Equal(4ul, contract.EndBlock);
            Assert.Equal(periods, contract.SalePeriods);
            this.mTransactionExecutor.Verify(m => m.Create<DividendToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, 0), Times.Once);
        }

        [Fact]
        public void Constructor_TokenTypeIsNonFungibleToken_Success()
        {
            this.mTransactionExecutor.Setup(m => m.Create<NonFungibleToken>(this.mContractState.Object, 0, new object[] { this.name, this.symbol }, It.IsAny<ulong>())).Returns(createSucceed());
            var (contract, periods) = this.Create(TokenType.NonFungibleToken);

            Assert.Equal((UInt256)ulong.MaxValue, contract.TokenBalance);
            Assert.Equal(this.owner, contract.Owner);
            Assert.Equal(this.tokenContract, contract.TokenAddress);
            Assert.Equal(4ul, contract.EndBlock);
            Assert.Equal(periods, contract.SalePeriods);
            this.mTransactionExecutor.Verify(m => m.Create<NonFungibleToken>(this.mContractState.Object, 0, new object[] { this.name, this.symbol }, 0), Times.Once);
        }

        public (STOContract contract, SalePeriod[] periods) Create(TokenType tokenType)
        {
            var periodInputs = new[]
            {
                new SalePeriodInput { PricePerToken = 3 * Satoshis, DurationBlocks = 1 },
                new SalePeriodInput { PricePerToken = 5 * Satoshis, DurationBlocks = 2 }
            };
            var periods = new[]
            {
                new SalePeriod { PricePerToken = 3 * Satoshis, EndBlock = 2 },
                new SalePeriod { PricePerToken = 5 * Satoshis, EndBlock = 4 }
            };

            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.owner, 0));
            this.mBlock.Setup(s => s.Number).Returns(1);
            this.mSerializer.Setup(m => m.ToArray<SalePeriodInput>(new byte[0])).Returns(periodInputs);
            var contract = new STOContract(this.mContractState.Object, this.owner, (uint)tokenType, this.totalSupply, this.name, this.symbol, this.decimals, this.kycContract, this.mapperContract, new byte[0]);
            return (contract, periods);
        }

        [Fact]
        public void Invest_CalledForStandardToken_Success()
        {
            var amount = 15 * Satoshis;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);

            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(StandardToken.TransferTo), new object[] { this.investor, (UInt256)5 }, It.IsAny<ulong>())).Returns(transferSucceed(true));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(this.identity));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.identity, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferSucceed(new byte[] { 1 }));

            Assert.True(contract.Invest());

            Assert.Equal(this.totalSupply - 5ul, contract.TokenBalance);
            this.mTransactionExecutor.Verify(s => s.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);

            this.mBlock.Setup(s => s.Number).Returns(4);
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(StandardToken.TransferTo), new object[] { this.investor, (UInt256)3 }, It.IsAny<ulong>())).Returns(transferSucceed(true));

            Assert.True(contract.Invest());

            Assert.Equal(this.totalSupply - 8ul, contract.TokenBalance);
            this.mTransactionExecutor.Verify(s => s.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);
        }

        [Fact]
        public void Invest_CalledForNonFungibleToken_Success()
        {
            var amount = 15 * Satoshis;
            var totalSupply = (UInt256)ulong.MaxValue;

            this.mTransactionExecutor.Setup(m => m.Create<NonFungibleToken>(this.mContractState.Object, 0, new object[] { this.name, this.symbol }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.NonFungibleToken);

            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(NonFungibleToken.MintAll), new object[] { this.investor, 5ul }, It.IsAny<ulong>())).Returns(transferSucceed(true));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(this.identity));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.identity, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferSucceed(new byte[] { 1 }));

            Assert.True(contract.Invest());

            Assert.Equal(totalSupply - 5, contract.TokenBalance);
            this.mTransactionExecutor.Verify(s => s.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);

            this.mBlock.Setup(s => s.Number).Returns(4);
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(NonFungibleToken.MintAll), new object[] { this.investor, 3ul }, It.IsAny<ulong>())).Returns(transferSucceed(true));

            Assert.True(contract.Invest());

            Assert.Equal(totalSupply - 8ul, contract.TokenBalance);
            this.mTransactionExecutor.Verify(s => s.Transfer(It.IsAny<ISmartContractState>(), It.IsAny<Address>(), It.IsAny<ulong>()), Times.Never);
        }

        [Fact]
        public void Invest_Refunds_Oversold_Tokens()
        {
            this.totalSupply = 60;
            var amount = 190 * Satoshis;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);

            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(StandardToken.TransferTo), new object[] { this.investor, this.totalSupply }, It.IsAny<ulong>())).Returns(transferSucceed(true));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(this.identity));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.identity, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferSucceed(new byte[] { 1 }));
            this.mTransactionExecutor.Setup(m => m.Transfer(this.mContractState.Object, this.investor, 10 * Satoshis)).Returns(transferSucceed());
            Assert.True(contract.Invest());

            Assert.Equal((UInt256)0, contract.TokenBalance); // All tokens are sold
            this.mTransactionExecutor.Verify(s => s.Transfer(this.mContractState.Object, this.investor, 10 * Satoshis), Times.Once);
        }

        [Fact]
        public void Invest_Fails_If_TokenBalance_Is_Zero()
        {
            var amount = 1 * Satoshis;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.investor, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferSucceed(new byte[] { 1 }));

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));
            this.persistentState.SetUInt256(nameof(STOContract.TokenBalance), 0);

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_EndBlock_Reached()
        {
            var amount = 1 * Satoshis;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);

            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));
            this.mBlock.Setup(s => s.Number).Returns(5);

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_Investment_Amount_Is_Zero()
        {
            var amount = 0ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_GetSecondaryAddress_Call_Fails()
        {
            var amount = 10ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferFailed());
            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_GetSecondaryAddress_Call_Returns_Zero_Address()
        {
            var amount = 10ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(Address.Zero));
            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_GetClaim_Call_Fails()
        {
            var amount = 10ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(this.identity));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.identity, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferFailed());
            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void Invest_Fails_If_GetClaim_Call_Returns_Null()
        {
            var amount = 10ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.mapperContract, 0, "GetSecondaryAddress", new object[] { this.investor }, It.IsAny<ulong>())).Returns(transferSucceed(this.identity));
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.kycContract, 0, "GetClaim", new object[] { this.identity, 3U /*shufti kyc*/ }, It.IsAny<ulong>())).Returns(transferSucceed(null));
            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));

            Assert.Throws<SmartContractAssertException>(() => contract.Invest());
        }

        [Fact]
        public void WithdrawFunds_Fails_If_Caller_Is_Not_Owner()
        {
            var amount = 0ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.investor, amount));
            this.mBlock.Setup(m => m.Number).Returns(5);

            Assert.Throws<SmartContractAssertException>(() => contract.WithdrawFunds());
        }

        [Fact]
        public void WithdrawFunds_Fails_If_Sale_Is_Open()
        {
            var amount = 0ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.owner, amount));
            this.mBlock.Setup(m => m.Number).Returns(4);

            Assert.Throws<SmartContractAssertException>(() => contract.WithdrawFunds());
        }

        [Fact]
        public void WithdrawFunds_Called_By_Owner_Success()
        {
            var amount = 0ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.owner, amount));
            this.mBlock.Setup(m => m.Number).Returns(4);

            Assert.Throws<SmartContractAssertException>(() => contract.WithdrawFunds());
        }

        [Fact]
        public void WithdrawTokens_Called_By_Owner_After_Sale_Is_Closed_Success()
        {
            var amount = 0ul;
            this.mTransactionExecutor.Setup(m => m.Create<StandardToken>(this.mContractState.Object, 0, new object[] { this.totalSupply, this.name, this.symbol, this.decimals }, It.IsAny<ulong>())).Returns(createSucceed());

            var (contract, _) = this.Create(TokenType.StandardToken);
            this.mContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.owner, amount));
            this.mBlock.Setup(m => m.Number).Returns(5);
            this.persistentState.SetUInt256(nameof(contract.TokenBalance), 100);
            this.mTransactionExecutor.Setup(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(StandardToken.TransferTo), new object[] { this.owner, (UInt256)100 }, It.IsAny<ulong>())).Returns(transferSucceed(true));

            var success = contract.WithdrawTokens();

            Assert.True(success);
            Assert.Equal((UInt256)0, this.persistentState.GetUInt256(nameof(contract.TokenBalance)));
            this.mTransactionExecutor.Verify(m => m.Call(this.mContractState.Object, this.tokenContract, 0, nameof(StandardToken.TransferTo), new object[] { this.owner, (UInt256)100 }, 0));
        }
    }
}
