import React, { Component } from 'react';

export class FetchData extends Component {
    static displayName = FetchData.name;

    constructor(props) {
        super(props);
        this.state = { coins: [], loading: true };
    }

    componentDidMount() {
        this.populateWeatherData();
    }

    static renderForecastsTable(coins) {

        function sendOrder(val1, val2, val3, val4, val5, val6) {
            console.log(JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6 }));
            let obj = JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6 });
            const requestOptions = {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: obj
            };
            fetch('weatherforecast', requestOptions)
                .then(response => response.json())
                .then(data => this.setState({ postId: data.id }));
        }

        return (
            <div>
                <h1>BTC/USDT Price: ${coins.btc}</h1>
                <h1>ETH/USDT Price: ${coins.eth}</h1>
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
                        {coins.coins.map(
                            coin =>
                                <tr key={coin.symbol}>
                                    <td>{coin.symbol}</td>
                                    <td>{coin.percentage}</td>
                                    <td>
                                        <input type="text" value={coin.quantity}/>
                                    </td>
                                    <td>
                                        <button className="btn btn-primary" onClick={(event) => sendOrder(coin.usdt, coin.btc, coin.price, coin.quantity, coin.firstQuantity, coin.lastPrice)}>Send Order</button>
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
            : FetchData.renderForecastsTable(this.state.coins);

        return (
            <div>
                <h1 id="tabelLabel" >Coins' Arbitrage</h1>
                {contents}
            </div>
        );
    }

    async populateWeatherData() {
        const response = await fetch('weatherforecast');
        const data = await response.json();
        this.setState({ coins: data, loading: false });
    }

}
