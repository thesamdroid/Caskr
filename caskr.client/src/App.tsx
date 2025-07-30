import { useEffect, useState } from 'react';
import './App.css';

interface Order {
    id: number;
    name: string;
    statusId: number;
}

interface Status {
    id: number;
    name: string;
}

function App() {
    const [orders, setOrders] = useState<Order[]>([]);
    const [statuses, setStatuses] = useState<Status[]>([]);

    useEffect(() => {
        void fetchOrders();
        void fetchStatuses();
    }, []);

    const getStatusName = (id: number): string => {
        const status = statuses.find((s) => s.id === id);
        return status ? status.name : id.toString();
    };

    return (
        <div>
            <h1>Orders</h1>
            <table className="table table-striped" aria-label="Orders table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
                    {orders.map((order) => (
                        <tr key={order.id}>
                            <td>{order.name}</td>
                            <td>{getStatusName(order.statusId)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );

    async function fetchOrders() {
        const response = await fetch('api/orders');
        if (response.ok) {
            setOrders(await response.json());
        }
    }

    async function fetchStatuses() {
        const response = await fetch('api/status');
        if (response.ok) {
            setStatuses(await response.json());
        }
    }
}

export default App;