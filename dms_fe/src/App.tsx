import { Routes, Route } from 'react-router-dom';
import { Layout } from '@/components/layout';
import { ToastContainer } from '@/components/ui';
import { PoolManager } from '@/pages/PoolManager';
import { POManager } from '@/pages/POManager';
import { WebSocketTest } from '@/pages/WebSocketTest';

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/pools" element={<PoolManager />} />
        <Route path="/po" element={<POManager />} />
        <Route path="/ws-test" element={<WebSocketTest />} />
        <Route path="/" element={<PoolManager />} />
      </Routes>
      <ToastContainer />
    </Layout>
  );
}
