import React from 'react';
import './RightPanel.css';

function RightPanel({ children }) {
  return (
    <div className="right-panel">
      {children}
    </div>
  );
}

export default RightPanel;
