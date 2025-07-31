import { useEffect } from 'react';
import './App.css';
import { fetchOrders } from './features/ordersSlice';
import { fetchStatuses } from './features/statusSlice';
import { useAppDispatch, useAppSelector } from './hooks';

function App() {
    const dispatch = useAppDispatch();
    const orders = useAppSelector((state) => state.orders.items);
    const statuses = useAppSelector((state) => state.statuses.items);

    useEffect(() => {
        dispatch(fetchOrders());
        dispatch(fetchStatuses());
    }, [dispatch]);

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
}

export default App;
