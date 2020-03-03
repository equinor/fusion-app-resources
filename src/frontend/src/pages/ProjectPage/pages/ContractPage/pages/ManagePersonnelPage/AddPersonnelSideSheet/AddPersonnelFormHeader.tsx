import * as React from 'react';
import * as classNames from 'classnames'
import * as styles from './styles.less'

type HeaderProps = {
  headers: string[];
}

const AddPersonnelFormHeader : React.FC<HeaderProps> = ({headers}) => {

  const cellClassName = classNames(styles.cell, styles.header);
  return (
    <>
      <div className={classNames(cellClassName, styles.expand)} >
        {headers.map((headerlabel) => (
          <div style={{flexGrow:1}}
                    key={headerlabel}
                    className={classNames(styles.header)}
                >
                    <span className={styles.label}>{headerlabel}</span>
                </div>
            ))}
      </div>
    </>
  );
}

export default AddPersonnelFormHeader