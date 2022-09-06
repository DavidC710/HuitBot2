import React, { Component } from 'react';

export class Counter extends Component {
    static displayName = Counter.name;

    constructor(props) {
        super(props);
        this.state = { currentCount: 0 };
        this.incrementCounter = this.incrementCounter.bind(this);
    }

    incrementCounter(type) {
        console.log(type);
        this.setState({
            currentCount: this.state.currentCount + 1
        });
    }

    render() {

        function sendLoop(type) {
            const top = document.getElementById('top').value
            const floor = document.getElementById('floor').value
            const quantity = document.getElementById('quantity').value

            let obj = JSON.stringify({ type: type, top: top, floor: floor, quantity: quantity });

            const requestOptions = {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: obj
            };
            fetch('loop', requestOptions)
                .then(response => response.json())
                .then(response => {
                    console.log(response.message);
                    alert(response.message);
                });
        }

        return (
            <div >
                <h2 className='rowC-elementA'>BTC Loop</h2>
                <br />
                <div className='rowC'>
                    <h2 className='rowC-elementA'>Top</h2>
                    <input id='top' style={{ textAlign: 'center', marginLeft: '30px' }} type="text" />
                    <h2 className='rowC-elementA'>Floor</h2>
                    <input id='floor' style={{ textAlign: 'center', marginLeft: '30px' }} type="text" />
                    <h2 className='rowC-elementA'>Quantity</h2>
                    <input id='quantity' style={{ textAlign: 'center', marginLeft: '30px' }} type="text" />
                </div>
                <br />
                <br />
                <button style={{ textAlign: 'center', marginLeft: '30px', width: '150px' }} className="btn btn-primary" onClick={(event) => sendLoop('BUY')}>Buy</button>
                <button style={{ textAlign: 'center', marginLeft: '30px', width: '150px' }} className="btn btn-primary" onClick={(event) => sendLoop('SELL')}>Sell</button>
            </div>
        );
    }
}
