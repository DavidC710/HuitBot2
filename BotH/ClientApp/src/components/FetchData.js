import React, { Component } from 'react';

export class FetchData extends Component {
    static displayName = FetchData.name;


    constructor(props) {
        super(props);
        this.state = { coins: [], coinsRender: [], loading: true, exchange: 'ftx' };
        this.onValueChange = this.onValueChange.bind(this);
    }

    onValueChange(event) {
        let coins = this.state.coins;
        let coinsRender = coins[0];

        this.setState({
            exchange: event.target.value,
            coinsRender: coinsRender,
        });
    }

    componentDidMount() {
        this.interval = setInterval(() => this.populateCoinsData(), 5000);
    }

    componentWillUnmount() {
        clearInterval(this.interval);
    }

    static renderCoinsTable(coinsRender, exchange, onChange, onReload) {

        function sendOrder(val1, val2, val3, val4, val5, val6, exchange, percentage) {
            console.log(percentage);
            let obj = JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6, exchange: exchange, percentage: percentage });
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
                <div className='rowC'>
                    <h4 className='rowC-element'>BTC/USDT Price: ${coinsRender.btc}</h4>
                    <h4 className='rowC-element'>ETH/USDT Price: ${coinsRender.eth}</h4>
                </div>
                {/*<div className="radio">*/}
                {/*    <label>*/}
                {/*        <input*/}
                {/*            type="radio"*/}
                {/*            value="binance"*/}
                {/*            checked={exchange === "binance"}*/}
                {/*            onChange={onChange}*/}
                {/*        />*/}
                {/*        Binance*/}
                {/*    </label>*/}
                {/*</div>*/}
                {/*<div className="radio">*/}
                {/*    <label>*/}
                {/*        <input*/}
                {/*            type="radio"*/}
                {/*            value="ftx"*/}
                {/*            checked={exchange === "ftx"}*/}
                {/*            onChange={onChange}*/}
                {/*        />*/}
                {/*        FTX*/}
                {/*    </label>*/}
                {/*</div>*/}
                <br />
                <table style={{ textAlign: 'center' }} className='table table-striped' aria-labelledby="tabelLabel">
                    <thead>
                        <tr>
                            <th className="tableTitle">Coin Name</th>
                            <th className="tableTitle">Arbitrage Percentage</th>
                            <th className="tableTitle">State</th>
                            <th className="tableTitle">Quantity</th>
                            <th className="tableTitle">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {coinsRender?.coins?.map(
                            coin =>
                                <tr key={coin.symbol}>
                                    <td className='tableData'>{coin.symbol}</td>
                                    <td className='tableData'>{coin.percentage}</td>
                                    
                                    <td>
                                        <div className="dot" style={{ backgroundColor: coin.hasOpendOrders == true ? '#D72C3A' : '#00740B' }}>
                                        </div>
                                    </td>
                                    <td>
                                        <input style={{ textAlign: 'center' }} type="text" defaultValue={coin.quantity} onChange={(event) => onValChange(coin, event)} />
                                    </td>
                                    <td>
                                        <button
                                            className="btn btn-primary"
                                            onClick={(event) => sendOrder(coin.usdt, coin.btc, coin.price, coin.quantity, coin.firstQuantity, coin.lastPrice, exchange, coin.percentage)}>
                                            Send Order
                                        </button>
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
            ? <p><em><div style={{color: '#FFFFFF'}}>Loading...</div></em></p>
            : FetchData.renderCoinsTable(this.state.coinsRender, this.state.exchange, this.onValueChange, this.componentDidMount);

        return (
            <div>
                {contents}
            </div>
        );
    }

    async populateCoinsData() {
        const response = await fetch('arbitrage');
        const data = await response.json();
        this.setState({ coins: data, coinsRender: data[0], loading: false });
    }

}
