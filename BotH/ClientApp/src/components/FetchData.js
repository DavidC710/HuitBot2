import React, { Component } from 'react';

export class FetchData extends Component {
    static displayName = FetchData.name;


    constructor(props) {
        super(props);
        this.state = { coins: [], coinsRender: [], loading: true, exchange: 'binance' };
        this.onValueChange = this.onValueChange.bind(this);
    }

    onValueChange(event) {
        let coins = this.state.coins;
        let coinsRender = event.target.value == 'binance' ? coins[0] : coins[1];

        this.setState({
            exchange: event.target.value,
            coinsRender: coinsRender,
        });
    }

    componentDidMount() {
        this.populateCoinsData();
    }

    static renderCoinsTable(coinsRender, exchange, onChange, onReload) {

        function sendOrder(val1, val2, val3, val4, val5, val6, exchange) {
            let obj = JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6, exchange: exchange });
            const requestOptions = {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: obj
            };
            fetch('arbitrage', requestOptions)
                .then(response => response.json())
                .then(response => {
                    console.log(response.message);
                    alert(response.message);
                });
        }

        function onValChange(e, current) {
            e.quantity = current.currentTarget.value;
        };

        return (
            <div>
                <br />
                <h1>BTC/USDT Price: ${coinsRender.btc}</h1>
                <h1>ETH/USDT Price: ${coinsRender.eth}</h1>
                <button className="btn btn-primary"
                    onClick={onReload}>Reload prices</button>
                <br />
                <div className="radio">
                    <label>
                        <input
                            type="radio"
                            value="binance"
                            checked={exchange === "binance"}
                            onChange={onChange}
                        />
                        Binance
                    </label>
                </div>
                <div className="radio">
                    <label>
                        <input
                            type="radio"
                            value="ftx"
                            checked={exchange === "ftx"}
                            onChange={onChange}
                        />
                        FTX
                    </label>
                </div>
                <br />
                <table className='table table-striped' aria-labelledby="tabelLabel">
                    <thead>
                        <tr>
                            <th className="tableTitle">Coin Name</th>
                            <th className="tableTitle">Arbitrage Percentage</th>
                            <th className="tableTitle">Quantity</th>
                            <th className="tableTitle">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {coinsRender?.coins?.map(
                            coin =>
                                <tr key={coin.symbol}>
                                    <td>{coin.symbol}</td>
                                    <td>{coin.percentage}</td>
                                    <td>
                                        <input type="text" defaultValue={coin.quantity} onChange={(event) => onValChange(coin, event)} />
                                    </td>
                                    <td>
                                        <button className="btn btn-primary"
                                            onClick={(event) => sendOrder(coin.usdt, coin.btc, coin.price, coin.quantity, coin.firstQuantity, coin.lastPrice, exchange)}
                                        >Send Order</button>
                                    </td>
                                </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : FetchData.renderCoinsTable(this.state.coinsRender, this.state.exchange, this.onValueChange, this.componentDidMount);

        return (
            <div>
                <h1 id="tabelLabel" >Coins' Arbitrage</h1>
                {contents}
            </div>
        );
    }

    async populateCoinsData() {
        const response = await fetch('arbitrage');
        const data = await response.json();
        this.setState({ coins: data, coinsRender: this.exchange == 'binance' ? data[0] : data[1], loading: false});
    }

}
