import React from 'react';
import './LeftPanel.css';

function LeftPanel({ children }) {
  return (
    <div className="left-panel">
      {children}
    </div>
  );
}

export default LeftPanel;
