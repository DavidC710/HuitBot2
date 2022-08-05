import React, { Component } from 'react';

export class FetchData extends Component {
    static displayName = FetchData.name;


    constructor(props) {
        super(props);
        this.state = { coins: [], loading: true, exchange: 'binance' };
        this.onValueChange = this.onValueChange.bind(this);
    }

    onValueChange(event) {
        this.setState({
            exchange: event.target.value
        });
    }

    componentDidMount() {
        this.populateCoinsData();
    }

    static renderCoinsTable(coins, exchange, handler) {

        function sendOrder(val1, val2, val3, val4, val5, val6) {
            let obj = JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6 });
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

        function reloadPage(e) {
            window.location.reload(false);
        };



        return (
            <div>
                <br />
                <h1>BTC/USDT Price: ${coins[1].btc}</h1>
                <h1>ETH/USDT Price: ${coins[1].eth}</h1>
                <button className="btn btn-primary"
                    onClick={(event) => reloadPage()}
                >Reload prices</button>
                <br />
                <div className="radio">
                    <label>
                        <input
                            type="radio"
                            value="binance"
                            checked={exchange === "binance"}
                            onChange={handler}
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
                            onChange={handler}
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
                        {coins[1].coins.map(
                            coin =>
                                <tr key={coin.symbol}>
                                    <td>{coin.symbol}</td>
                                    <td>{coin.percentage}</td>
                                    <td>
                                        <input type="text" defaultValue={coin.quantity} onChange={(event) => onValChange(coin, event)} />
                                    </td>
                                    <td>
                                        <button className="btn btn-primary"
                                            onClick={(event) => sendOrder(coin.usdt, coin.btc, coin.price, coin.quantity, coin.firstQuantity, coin.lastPrice)}
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
            : FetchData.renderCoinsTable(this.state.coins, this.state.exchange, this.onValueChange);

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
        this.setState({ coins: data, loading: false, exchange: 'binance'});
    }

}
