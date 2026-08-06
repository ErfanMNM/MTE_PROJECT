import { Routes, Route } from 'react-router-dom';
import { Layout } from '@/components/layout';
import { ToastContainer } from '@/components/ui';
import { PoolManager } from '@/pages/PoolManager';
import { POManager } from '@/pages/POManager';

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/pools" element={<PoolManager />} />
        <Route path="/po" element={<POManager />} />
        <Route path="/" element={<PoolManager />} />
      </Routes>
      <ToastContainer />
    </Layout>
  );
}
